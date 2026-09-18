using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Variable.Database;
using Variable.Identity;

namespace Variable.Licensing;

public sealed record LicenseStatusView(string Status, string? LicenseId, long? Revision, int? MaxActiveLearners,
    int ActiveLearners, int? Remaining, DateTimeOffset? NotBefore, DateTimeOffset? ExpiresAt);

internal sealed class LicensingFailure(string code, int status = 400) : Exception(code)
{
    internal string Code { get; } = code;
    internal int Status { get; } = status;
}

public static class LicensingModule
{
    public static ModuleMigration Migration => ModuleMigration.Embedded(typeof(LicensingModule).Assembly, 4, "0004-licensing", "licensing", """
        GRANT USAGE ON SCHEMA licensing TO {runtime_role};
        GRANT SELECT, INSERT ON licensing.license_documents TO {runtime_role};
        GRANT SELECT, INSERT, UPDATE ON licensing.license_state TO {runtime_role};
        """);

    public static IServiceCollection AddVariableLicensing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(LicenseTrust.Read(configuration));
        services.AddScoped<LicenseService>();
        services.AddScoped<ILearnerCapacityPolicy, LicenseCapacityPolicy>();
        return services;
    }

    public static IEndpointRouteBuilder MapVariableLicensing(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/licensing").RequireAuthorization().AddEndpointFilter(async (context, next) =>
        {
            try { return await next(context); }
            catch (LicensingFailure failure)
            { return Results.Json(new { code = failure.Code, requestId = context.HttpContext.TraceIdentifier }, statusCode: failure.Status); }
        });
        api.MapGet("/status", async (LicenseService service, HttpContext context) =>
            Results.Ok(await service.StatusAsync(context.User, context.RequestAborted)));
        api.MapPost("/license", async (LoadLicenseRequest request, IAntiforgery antiforgery, LicenseService service, HttpContext context) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            return Results.Ok(await service.LoadAsync(context.User, request.CompactJws, context.RequestAborted));
        });
        return endpoints;
    }

    private sealed record LoadLicenseRequest(string CompactJws);
}

internal sealed class LicenseService(IIdentityAccess identity, LicenseTrust trust, TimeProvider clock)
{
    internal async Task<LicenseStatusView> LoadAsync(System.Security.Claims.ClaimsPrincipal principal, string compactJws, CancellationToken ct)
    {
        var verified = await LicenseVerifier.VerifyAsync(compactJws, trust, clock.GetUtcNow());
        await using var work = await identity.BeginWriteAsync(principal, ct);
        if (!work.Actor.Account.Roles.Contains(nameof(AccountRole.Administrator))) throw new LicensingFailure("forbidden", 403);
        if (verified.OrganizationId != work.Actor.Organization.Id) throw new LicensingFailure("license_organization_mismatch");

        var current = await CurrentAsync(work.Connection, work.Transaction, work.Actor.Organization.Id, ct);
        if (current is not null)
        {
            if (current.Value.Revision == verified.Revision && current.Value.Hash == verified.Hash)
                return await BuildStatusAsync(work.Connection, work.Transaction, work.Actor.Organization.Id, current.Value, clock.GetUtcNow(), ct);
            if (verified.Revision <= current.Value.Revision) throw new LicensingFailure("license_revision_replayed", 409);
        }
        var usage = await ActiveLearnersAsync(work.Connection, work.Transaction, work.Actor.Organization.Id, ct);
        if (verified.MaxActiveLearners < usage) throw new LicensingFailure("license_downsize_not_supported", 409);

        await using (var insert = new NpgsqlCommand("""
            INSERT INTO licensing.license_documents(organization_id, revision, license_id, schema_version, key_id,
                issued_at, not_before, expires_at, max_active_learners, compact_jws, document_hash, loaded_by, loaded_at)
            VALUES ($1,$2,$3,1,$4,$5,$6,$7,$8,$9,$10,$11,$12)
            """, (NpgsqlConnection)work.Connection, (NpgsqlTransaction)work.Transaction))
        {
            insert.Parameters.AddWithValue(work.Actor.Organization.Id); insert.Parameters.AddWithValue(verified.Revision);
            insert.Parameters.AddWithValue(verified.LicenseId); insert.Parameters.AddWithValue(verified.KeyId);
            insert.Parameters.AddWithValue(verified.IssuedAt); insert.Parameters.AddWithValue(verified.NotBefore);
            insert.Parameters.AddWithValue(verified.ExpiresAt); insert.Parameters.AddWithValue(verified.MaxActiveLearners);
            insert.Parameters.AddWithValue(compactJws); insert.Parameters.AddWithValue(verified.Hash);
            insert.Parameters.AddWithValue(work.Actor.Account.Id); insert.Parameters.AddWithValue(clock.GetUtcNow());
            await insert.ExecuteNonQueryAsync(ct);
        }
        await using (var state = new NpgsqlCommand("""
            INSERT INTO licensing.license_state(organization_id, current_revision) VALUES ($1,$2)
            ON CONFLICT (organization_id) DO UPDATE SET current_revision = excluded.current_revision
            """, (NpgsqlConnection)work.Connection, (NpgsqlTransaction)work.Transaction))
        {
            state.Parameters.AddWithValue(work.Actor.Organization.Id); state.Parameters.AddWithValue(verified.Revision);
            await state.ExecuteNonQueryAsync(ct);
        }
        await work.AuditAsync("license.loaded", work.Actor.Organization.Id,
            new { verified.LicenseId, verified.Revision, verified.MaxActiveLearners, verified.ExpiresAt, verified.KeyId }, ct);
        await work.CommitAsync(ct);
        return new("valid", verified.LicenseId, verified.Revision, verified.MaxActiveLearners, usage,
            verified.MaxActiveLearners - usage, verified.NotBefore, verified.ExpiresAt);
    }

    internal async Task<LicenseStatusView> StatusAsync(System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct)
    {
        await using var work = await identity.BeginWriteAsync(principal, ct);
        if (!work.Actor.Account.Roles.Intersect([nameof(AccountRole.Administrator), nameof(AccountRole.OrganizationManager)]).Any())
            throw new LicensingFailure("forbidden", 403);
        var current = await CurrentAsync(work.Connection, work.Transaction, work.Actor.Organization.Id, ct);
        var usage = await ActiveLearnersAsync(work.Connection, work.Transaction, work.Actor.Organization.Id, ct);
        return current is null
            ? new("missing", null, null, null, usage, null, null, null)
            : await BuildStatusAsync(work.Connection, work.Transaction, work.Actor.Organization.Id, current.Value, clock.GetUtcNow(), ct);
    }

    private static async Task<LicenseStatusView> BuildStatusAsync(System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction, Guid organizationId, CurrentLicense current, DateTimeOffset now, CancellationToken ct)
    {
        var usage = await ActiveLearnersAsync(connection, transaction, organizationId, ct);
        var status = now < current.NotBefore ? "not_yet_valid" : now >= current.ExpiresAt ? "expired" : "valid";
        return new(status, current.LicenseId, current.Revision, current.Maximum, usage,
            Math.Max(0, current.Maximum - usage), current.NotBefore, current.ExpiresAt);
    }

    internal static async Task<int> ActiveLearnersAsync(System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction, Guid organizationId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT count(*)::integer FROM identity.users
            WHERE organization_id=$1 AND status='active' AND deleted_at IS NULL AND 'Learner'=ANY(roles)
            """, (NpgsqlConnection)connection, (NpgsqlTransaction)transaction);
        command.Parameters.AddWithValue(organizationId);
        return (int)(await command.ExecuteScalarAsync(ct))!;
    }

    internal static async Task<CurrentLicense?> CurrentAsync(System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction, Guid organizationId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT d.revision,d.license_id,d.document_hash,d.max_active_learners,d.not_before,d.expires_at
            FROM licensing.license_state s JOIN licensing.license_documents d
              ON d.organization_id=s.organization_id AND d.revision=s.current_revision
            WHERE s.organization_id=$1
            """, (NpgsqlConnection)connection, (NpgsqlTransaction)transaction);
        command.Parameters.AddWithValue(organizationId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
            reader.GetInt32(3), reader.GetFieldValue<DateTimeOffset>(4), reader.GetFieldValue<DateTimeOffset>(5)) : null;
    }

    internal readonly record struct CurrentLicense(long Revision, string LicenseId, string Hash, int Maximum,
        DateTimeOffset NotBefore, DateTimeOffset ExpiresAt);
}

internal sealed class LicenseCapacityPolicy(TimeProvider clock) : ILearnerCapacityPolicy
{
    public async Task<LearnerCapacityDecision> ValidateIncreaseAsync(IdentityWriteContext context, int currentUsage, int increase, CancellationToken ct)
    {
        if (increase <= 0) return LearnerCapacityDecision.Permit;
        var license = await LicenseService.CurrentAsync(context.Connection, context.Transaction, context.Actor.Organization.Id, ct);
        if (license is null) return LearnerCapacityDecision.Reject("license_missing");
        var now = clock.GetUtcNow();
        if (now < license.Value.NotBefore) return LearnerCapacityDecision.Reject("license_not_yet_valid");
        if (now >= license.Value.ExpiresAt) return LearnerCapacityDecision.Reject("license_expired");
        return currentUsage + increase <= license.Value.Maximum
            ? LearnerCapacityDecision.Permit : LearnerCapacityDecision.Reject("learner_capacity_full");
    }
}

internal sealed record LicenseTrust(IReadOnlyDictionary<string, string> PublicKeyPaths)
{
    internal static LicenseTrust Read(IConfiguration configuration)
    {
        var keys = configuration.GetSection("Licensing:TrustedKeys").GetChildren()
            .Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value!, StringComparer.Ordinal);
        foreach (var path in keys.Values)
            if (!Path.IsPathFullyQualified(path) || !File.Exists(path))
                throw new InvalidOperationException("Every Licensing:TrustedKeys entry must name an existing absolute PEM public-key path.");
        return new(keys);
    }
}

internal sealed record VerifiedLicense(Guid OrganizationId, string LicenseId, long Revision, string KeyId,
    DateTimeOffset IssuedAt, DateTimeOffset NotBefore, DateTimeOffset ExpiresAt, int MaxActiveLearners, string Hash);

internal static class LicenseVerifier
{
    internal static async Task<VerifiedLicense> VerifyAsync(string compact, LicenseTrust trust, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(compact) || compact.Length > 32_768) throw new LicensingFailure("invalid_license_format");
        var parts = compact.Split('.');
        if (parts.Length != 3 || parts.Any(string.IsNullOrEmpty)) throw new LicensingFailure("invalid_license_format");
        byte[] headerBytes, payloadBytes, signature;
        try { headerBytes = Decode(parts[0]); payloadBytes = Decode(parts[1]); signature = Decode(parts[2]); }
        catch (FormatException) { throw new LicensingFailure("invalid_license_format"); }
        using var header = Parse(headerBytes); using var payload = Parse(payloadBytes);
        RejectDuplicates(header.RootElement); RejectDuplicates(payload.RootElement);
        RequireOnly(header.RootElement, ["alg", "kid", "typ"]);
        if (Text(header.RootElement, "alg") != "ES256" || Text(header.RootElement, "typ") != "variable-lms-license")
            throw new LicensingFailure("unsupported_license_header");
        var kid = Text(header.RootElement, "kid");
        if (!trust.PublicKeyPaths.TryGetValue(kid, out var keyPath)) throw new LicensingFailure("unknown_license_key");
        if (signature.Length != 64) throw new LicensingFailure("invalid_license_signature");
        using var key = ECDsa.Create();
        try { key.ImportFromPem(File.ReadAllText(keyPath)); }
        catch (CryptographicException) { throw new InvalidOperationException("Configured licensing public key is not a valid EC PEM key."); }
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(compact, new TokenValidationParameters
        {
            RequireSignedTokens = true,
            RequireExpirationTime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new ECDsaSecurityKey(key)
            {
                KeyId = kid,
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            },
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
            ValidTypes = ["variable-lms-license"],
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        });
        if (!validation.IsValid)
            throw new LicensingFailure("invalid_license_signature");

        var root = payload.RootElement;
        RequireOnly(root, ["schema_version", "product", "license_id", "organization_id", "revision", "issued_at", "not_before", "expires_at", "entitlements", "required_entitlements", "extensions"]);
        if (Integer(root, "schema_version") != 1) throw new LicensingFailure("unsupported_license_schema");
        if (Text(root, "product") != "variable-lms") throw new LicensingFailure("license_product_mismatch");
        if (!Guid.TryParse(Text(root, "organization_id"), out var organization) || organization == Guid.Empty)
            throw new LicensingFailure("invalid_license_organization");
        var licenseId = Text(root, "license_id");
        if (licenseId.Length is < 1 or > 200) throw new LicensingFailure("invalid_license_id");
        var revision = Long(root, "revision"); if (revision <= 0) throw new LicensingFailure("invalid_license_revision");
        var issued = Time(root, "issued_at"); var notBefore = Time(root, "not_before"); var expires = Time(root, "expires_at");
        if (notBefore >= expires || issued > now.AddMinutes(5)) throw new LicensingFailure("invalid_license_interval");
        if (now < notBefore) throw new LicensingFailure("license_not_yet_valid", 409);
        if (now >= expires) throw new LicensingFailure("license_expired", 409);
        var entitlements = Object(root, "entitlements");
        var maximumLong = Long(entitlements, "max_active_learners");
        if (maximumLong is < 0 or > 10_000_000) throw new LicensingFailure("invalid_learner_capacity");
        if (!root.TryGetProperty("required_entitlements", out var required)) throw new LicensingFailure("invalid_license_payload");
        if (required.ValueKind != JsonValueKind.Array) throw new LicensingFailure("invalid_license_payload");
        var requiredNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in required.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) throw new LicensingFailure("invalid_license_payload");
            var name = item.GetString()!;
            if (!requiredNames.Add(name)) throw new LicensingFailure("invalid_license_payload");
            if (name != "max_active_learners") throw new LicensingFailure("unsupported_required_entitlement");
        }
        if (root.TryGetProperty("extensions", out var extensions) && extensions.ValueKind != JsonValueKind.Object)
            throw new LicensingFailure("invalid_license_payload");
        return new(organization, licenseId, revision, kid, issued, notBefore, expires, (int)maximumLong,
            Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(compact))));
    }

    private static JsonDocument Parse(byte[] bytes)
    {
        try { return JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 16, CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false }); }
        catch (JsonException) { throw new LicensingFailure("invalid_license_json"); }
    }
    private static byte[] Decode(string value)
    {
        if (value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))) throw new FormatException();
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", 0 => "", _ => throw new FormatException() };
        return Convert.FromBase64String(padded);
    }
    private static void RejectDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            { if (!names.Add(property.Name)) throw new LicensingFailure("duplicate_license_property"); RejectDuplicates(property.Value); }
        }
        else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) RejectDuplicates(item);
    }
    private static void RequireOnly(JsonElement element, string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new LicensingFailure("invalid_license_payload");
        var allowed = names.ToHashSet(StringComparer.Ordinal);
        if (element.EnumerateObject().Any(x => !allowed.Contains(x.Name))) throw new LicensingFailure("unsupported_license_field");
    }
    private static JsonElement Object(JsonElement element, string name)
    { return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : throw new LicensingFailure("invalid_license_payload"); }
    private static string Text(JsonElement element, string name)
    { return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()! : throw new LicensingFailure("invalid_license_payload"); }
    private static int Integer(JsonElement element, string name)
    { return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result) ? result : throw new LicensingFailure("invalid_license_payload"); }
    private static long Long(JsonElement element, string name)
    { return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var result) ? result : throw new LicensingFailure("invalid_license_payload"); }
    private static DateTimeOffset Time(JsonElement element, string name)
    {
        var text = Text(element, name);
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) || value.Offset != TimeSpan.Zero)
            throw new LicensingFailure("invalid_license_time");
        return value;
    }
}

using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Variable.Identity.Persistence;
using Variable.Database;
using Npgsql;

namespace Variable.Identity;

internal sealed class IdentityFailure(string code, int status) : Exception(code)
{
    internal string Code { get; } = code;
    internal int Status { get; } = status;
}

internal static class IdentityClaims
{
    internal const string Scheme = "Variable.Session";
    internal const string Organization = "variable:organization";
    internal const string Version = "variable:security-version";
    internal static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    internal static long SecurityVersion(ClaimsPrincipal principal) => long.Parse(principal.FindFirstValue(Version)!, System.Globalization.CultureInfo.InvariantCulture);
}

internal sealed class IdentityService(IdentityDb db, InstallationOptions installation, IPasswordHasher<UserAccount> hasher, TimeProvider clock,
    MigrationPlan migrations, IEnumerable<IAccountAccessRemovalGuard> removalGuards,
    ILearnerCapacityPolicy learnerCapacity, IInvitationEmailWriter invitationEmail)
{
    internal async Task BootstrapAsync(string organizationName, string name, string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(organizationName) || organizationName.Length > 200 || string.IsNullOrWhiteSpace(name) || name.Length > 200
            || email.Length > 320 || !MailAddress.TryCreate(email, out var address) || address.Address != email
            || password.Length < 14 || password.Length > 128)
            throw new IdentityFailure("invalid_bootstrap_input", 400);
        if (!await DatabaseLifecycle.ReadyAsync(installation, migrations, ct)) throw new IdentityFailure("database_not_ready", 503);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // One installation, independent of the requested organization ID, even under concurrent commands.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(817601002)", ct);
        var bootstrapped = await db.Database.SqlQueryRaw<bool>("SELECT EXISTS (SELECT FROM identity.installation) AS \"Value\"").SingleAsync(ct);
        if (bootstrapped) throw new IdentityFailure("already_bootstrapped", 409);
        var now = clock.GetUtcNow();
        var user = new UserAccount
        {
            Id = Guid.NewGuid(), OrganizationId = installation.OrganizationId, Name = name.Trim(), Email = email,
            NormalizedEmail = email.ToUpperInvariant(), Roles = [nameof(AccountRole.Administrator)], CreatedAt = now, UpdatedAt = now
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Organizations.Add(new Organization { Id = installation.OrganizationId, Name = organizationName.Trim(), CreatedAt = now });
        db.Users.Add(user);
        db.Audit.Add(new AuditEntry
        {
            OrganizationId = installation.OrganizationId, ActorKind = "operator", Action = "organization.bootstrapped",
            TargetId = user.Id, CreatedAt = now, Metadata = "{\"roles\":[\"Administrator\"]}"
        });
        await db.SaveChangesAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO identity.installation(organization_id, bootstrapped_at) VALUES ({installation.OrganizationId}, {now})", ct);
        await transaction.CommitAsync(ct);
    }

    internal async Task<ClaimsPrincipal> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320 || string.IsNullOrEmpty(password) || password.Length > 128)
            throw new IdentityFailure("invalid_credentials", 401);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var normalized = email.ToUpperInvariant();
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM identity.users WHERE organization_id = {installation.OrganizationId} AND normalized_email = {normalized} FOR UPDATE").SingleOrDefaultAsync(ct);
        var now = clock.GetUtcNow();
        if (user is null || !user.Active || user.LockoutEnd > now)
        {
            // Same expensive password check for unknown/disabled users; no account-existence response.
            hasher.VerifyHashedPassword(new UserAccount(), DummyPasswordHash.Value, password);
            throw new IdentityFailure("invalid_credentials", 401);
        }
        var outcome = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (outcome == PasswordVerificationResult.Failed)
        {
            user.FailedAccessCount++;
            if (user.FailedAccessCount >= 5)
            {
                user.LockoutEnd = now.AddMinutes(15);
                user.FailedAccessCount = 0;
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw new IdentityFailure("invalid_credentials", 401);
        }
        if (outcome == PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash = hasher.HashPassword(user, password);
        user.FailedAccessCount = 0;
        user.LockoutEnd = null;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.Name),
            new(IdentityClaims.Organization, user.OrganizationId.ToString()),
            new(IdentityClaims.Version, user.SecurityVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityClaims.Scheme));
    }

    private static readonly Lazy<string> DummyPasswordHash = new(() => new PasswordHasher<UserAccount>().HashPassword(new UserAccount(), Guid.NewGuid().ToString()));

    internal async Task<SessionView> CurrentAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var user = await RequireActorAsync(principal, ct);
        var organization = await db.Organizations.AsNoTracking().SingleAsync(x => x.Id == installation.OrganizationId, ct);
        return new(user.View(), new(organization.Id, organization.Name));
    }

    internal async Task ChangeAdministratorAsync(ClaimsPrincipal principal, Guid targetId, bool grant, string password, string reason, CancellationToken ct)
    {
        ValidateReason(reason);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockOrganizationAsync(ct);
        var actor = await RequireActorAsync(principal, ct);
        if (!actor.Administrator) throw new IdentityFailure("forbidden", 403);
        Reauthenticate(actor, password);
        var target = await RequireTargetAsync(targetId, ct);
        if (grant && (!target.Active || string.IsNullOrEmpty(target.PasswordHash) || target.LockoutEnd > clock.GetUtcNow()))
            throw new IdentityFailure("administrator_must_be_usable", 409);
        if (target.Administrator == grant) return; // Natural retry; no duplicate audit or invalidation.
        if (!grant) await GuardAdministratorContinuityAsync(target, ct);
        var before = target.Roles;
        target.Roles = grant ? [.. target.Roles, nameof(AccountRole.Administrator)] : target.Roles.Where(x => x != nameof(AccountRole.Administrator)).ToArray();
        await RecordSecurityChangeAsync(actor, target, grant ? "administrator.granted" : "administrator.revoked", new { before, after = target.Roles, reason }, ct);
        await transaction.CommitAsync(ct);
    }

    internal async Task DeactivateAsync(ClaimsPrincipal principal, Guid targetId, string? password, string reason, CancellationToken ct)
    {
        ValidateReason(reason);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockOrganizationAsync(ct);
        var actor = await RequireActorAsync(principal, ct);
        var target = await RequireTargetAsync(targetId, ct);
        if (!actor.Administrator && (!actor.Roles.Contains(nameof(AccountRole.OrganizationManager))
                || target.Roles.Intersect([nameof(AccountRole.Administrator), nameof(AccountRole.OrganizationManager)]).Any()))
            throw new IdentityFailure("forbidden", 403);
        if (target.Administrator)
        {
            Reauthenticate(actor, password);
            await GuardAdministratorContinuityAsync(target, ct);
        }
        if (target.Status == "deactivated") return;
        var organization = await db.Organizations.AsNoTracking().SingleAsync(x => x.Id == installation.OrganizationId, ct);
        var context = new IdentityWriteContext(new(actor.View(), new(organization.Id, organization.Name)),
            db.Database.GetDbConnection(), transaction.GetDbTransaction());
        foreach (var guard in removalGuards)
            if (await guard.ValidateDeactivationAsync(context, target.Id, ct) is { } rejection) throw new IdentityFailure(rejection, 409);
        var before = target.Status;
        target.Status = "deactivated";
        await RecordSecurityChangeAsync(actor, target, "user.deactivated", new { before, after = target.Status, reason }, ct);
        await transaction.CommitAsync(ct);
    }

    internal async Task<ProvisioningAccountView[]> FindAccountsAsync(ClaimsPrincipal principal, string search, CancellationToken ct)
    {
        var actor = await RequireActorAsync(principal, ct);
        if (!actor.Administrator && !actor.Roles.Contains(nameof(AccountRole.OrganizationManager))) throw new IdentityFailure("forbidden", 403);
        var users = await db.Users.AsNoTracking().Where(x => x.OrganizationId == installation.OrganizationId && x.DeletedAt == null
            && x.NormalizedEmail.Contains(search.ToUpperInvariant())).OrderBy(x => x.NormalizedEmail).ThenBy(x => x.Id).Take(50).ToListAsync(ct);
        var ids = users.Select(x => x.Id).ToArray();
        var expiries = new Dictionary<Guid, DateTimeOffset>();
        if (ids.Length > 0)
        {
            await using var connection = new NpgsqlConnection(installation.ConnectionString); await connection.OpenAsync(ct);
            await using var command = new NpgsqlCommand("SELECT user_id,expires_at FROM identity.invitations WHERE organization_id=$1 AND user_id=ANY($2) AND used_at IS NULL", connection);
            command.Parameters.AddWithValue(installation.OrganizationId); command.Parameters.AddWithValue(ids);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) expiries[reader.GetGuid(0)] = reader.GetFieldValue<DateTimeOffset>(1);
        }
        return users.Select(x => new ProvisioningAccountView(x.Id, x.Name, x.Email, x.Status, x.Roles,
            expiries.GetValueOrDefault(x.Id) is var expiry && expiry != default ? expiry : null)).ToArray();
    }

    internal async Task<InvitationView> InviteAsync(ClaimsPrincipal principal, InviteAccount request, CancellationToken ct)
    {
        if (request.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200
            || request.Email.Length > 320 || !MailAddress.TryCreate(request.Email, out var address) || address.Address != request.Email
            || request.Roles.Length is < 1 or > 5 || request.Roles.Distinct().Count() != request.Roles.Length
            || request.Roles.Any(x => !Enum.IsDefined(x) || x == AccountRole.Administrator))
            throw new IdentityFailure("invalid_invitation", 400);
        var normalized = request.Email.ToUpperInvariant();
        var requestHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        { name = request.Name.Trim(), email = normalized, roles = request.Roles.Order().Select(x => x.ToString()).ToArray() })));
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockOrganizationAsync(ct);
        var actor = await RequireActorAsync(principal, ct);
        var actorRoles = actor.Roles.Select(Enum.Parse<AccountRole>).ToArray();
        if (request.Roles.Any(role => !RoleGrantPolicy.CanGrantOrRevoke(actorRoles, role))) throw new IdentityFailure("forbidden", 403);
        var priorRequest = await InvitationByRequestAsync(request.RequestId, ct);
        if (priorRequest is not null)
        {
            if (priorRequest.RequestHash != requestHash) throw new IdentityFailure("request_id_conflict", 409);
            return new(priorRequest.UserId, "pending", priorRequest.ExpiresAt);
        }
        var user = await db.Users.SingleOrDefaultAsync(x => x.OrganizationId == installation.OrganizationId && x.NormalizedEmail == normalized, ct);
        if (user is not null && user.Status != "pending") throw new IdentityFailure("account_already_exists", 409);
        InvitationRow? existing = user is null ? null : await InvitationAsync(user.Id, true, ct);
        if (existing is not null && existing.RequestId == request.RequestId)
        {
            if (existing.RequestHash != requestHash) throw new IdentityFailure("request_id_conflict", 409);
            return new(user!.Id, "pending", existing.ExpiresAt);
        }
        if (request.Roles.Contains(AccountRole.Learner)) await RequireCapacityAsync(actor, transaction, 1, ct);
        var now = clock.GetUtcNow(); var expires = now.AddHours(72); var version = (existing?.Version ?? 0) + 1;
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tokenHash = HashToken(token);
        if (user is null)
        {
            user = new UserAccount { Id = Guid.NewGuid(), OrganizationId = installation.OrganizationId, Name = request.Name.Trim(),
                Email = request.Email, NormalizedEmail = normalized, PasswordHash = "", Status = "pending",
                Roles = request.Roles.Select(x => x.ToString()).ToArray(), CreatedAt = now, UpdatedAt = now };
            db.Users.Add(user); await db.SaveChangesAsync(ct);
        }
        else
        {
            user.Name = request.Name.Trim(); user.Email = request.Email;
            user.Roles = request.Roles.Select(x => x.ToString()).ToArray(); user.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
        }
        await using (var command = new NpgsqlCommand("""
            INSERT INTO identity.invitations(organization_id,user_id,token_hash,expires_at,version,issued_by,request_id,request_hash,created_at,updated_at)
            VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$9)
            ON CONFLICT (organization_id,user_id) DO UPDATE SET token_hash=excluded.token_hash,expires_at=excluded.expires_at,
              used_at=NULL,version=excluded.version,issued_by=excluded.issued_by,request_id=excluded.request_id,
              request_hash=excluded.request_hash,updated_at=excluded.updated_at
            """, (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)transaction.GetDbTransaction()))
        {
            command.Parameters.AddWithValue(installation.OrganizationId); command.Parameters.AddWithValue(user.Id);
            command.Parameters.AddWithValue(tokenHash); command.Parameters.AddWithValue(expires); command.Parameters.AddWithValue(version);
            command.Parameters.AddWithValue(actor.Id); command.Parameters.AddWithValue(request.RequestId);
            command.Parameters.AddWithValue(requestHash); command.Parameters.AddWithValue(now); await command.ExecuteNonQueryAsync(ct);
        }
        var organization = await db.Organizations.AsNoTracking().SingleAsync(x => x.Id == installation.OrganizationId, ct);
        var context = new IdentityWriteContext(new(actor.View(), new(organization.Id, organization.Name)),
            db.Database.GetDbConnection(), transaction.GetDbTransaction());
        await invitationEmail.EnqueueAsync(context, user.Id, user.Email, token, expires, version, ct);
        db.Audit.Add(new AuditEntry { OrganizationId = installation.OrganizationId, ActorUserId = actor.Id,
            Action = existing is null ? "user.invited" : "invitation.resent", TargetId = user.Id,
            Metadata = JsonSerializer.Serialize(new { roles = user.Roles, version }), CreatedAt = now });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return new(user.Id, "pending", expires);
    }

    internal async Task AcceptInvitationAsync(string token, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512 || password.Length is < 14 or > 128)
            throw new IdentityFailure("invalid_invitation_token", 400);
        await using var transaction = await db.Database.BeginTransactionAsync(ct); await LockOrganizationAsync(ct);
        var invitation = await InvitationByTokenAsync(HashToken(token), ct);
        if (invitation is null || invitation.UsedAt is not null || invitation.ExpiresAt <= clock.GetUtcNow())
            throw new IdentityFailure("invalid_invitation_token", 400);
        var user = await RequireTargetAsync(invitation.UserId, ct);
        if (user.Status != "pending") throw new IdentityFailure("invalid_invitation_token", 400);
        var issuer = await RequireTargetAsync(invitation.IssuedBy, ct);
        var issuerRoles = issuer.Roles.Select(Enum.Parse<AccountRole>).ToArray();
        if (!issuer.Active || user.Roles.Select(Enum.Parse<AccountRole>).Any(role => role == AccountRole.Administrator
            || !RoleGrantPolicy.CanGrantOrRevoke(issuerRoles, role))) throw new IdentityFailure("invitation_authority_revoked", 409);
        if (user.Roles.Contains(nameof(AccountRole.Learner))) await RequireCapacityAsync(issuer, transaction, 1, ct);
        user.PasswordHash = hasher.HashPassword(user, password); user.Status = "active"; user.SecurityVersion++;
        var acceptedAt = clock.GetUtcNow(); user.UpdatedAt = acceptedAt;
        await using (var command = new NpgsqlCommand("UPDATE identity.invitations SET used_at=$1,updated_at=$1 WHERE organization_id=$2 AND user_id=$3", (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)transaction.GetDbTransaction()))
        { command.Parameters.AddWithValue(acceptedAt); command.Parameters.AddWithValue(installation.OrganizationId); command.Parameters.AddWithValue(user.Id); await command.ExecuteNonQueryAsync(ct); }
        db.Audit.Add(new AuditEntry { OrganizationId = installation.OrganizationId, ActorKind = "system", Action = "invitation.accepted",
            TargetId = user.Id, Metadata = JsonSerializer.Serialize(new { roles = user.Roles, invitation.Version }), CreatedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }

    internal async Task ReactivateAsync(ClaimsPrincipal principal, Guid targetId, string reason, CancellationToken ct)
    {
        ValidateReason(reason); await using var transaction = await db.Database.BeginTransactionAsync(ct); await LockOrganizationAsync(ct);
        var actor = await RequireActorAsync(principal, ct); var target = await RequireTargetAsync(targetId, ct);
        if (!actor.Administrator && (!actor.Roles.Contains(nameof(AccountRole.OrganizationManager))
            || target.Roles.Intersect([nameof(AccountRole.Administrator), nameof(AccountRole.OrganizationManager)]).Any()))
            throw new IdentityFailure("forbidden", 403);
        if (target.Status == "active") return;
        if (target.Status != "deactivated" || string.IsNullOrEmpty(target.PasswordHash)) throw new IdentityFailure("account_not_reactivatable", 409);
        if (target.Roles.Contains(nameof(AccountRole.Learner))) await RequireCapacityAsync(actor, transaction, 1, ct);
        var before = target.Status; target.Status = "active";
        await RecordSecurityChangeAsync(actor, target, "user.reactivated", new { before, after = target.Status, reason }, ct);
        await transaction.CommitAsync(ct);
    }

    internal async Task RestoreAsync(ClaimsPrincipal principal, Guid targetId, string reason, CancellationToken ct)
    {
        ValidateReason(reason); await using var transaction = await db.Database.BeginTransactionAsync(ct); await LockOrganizationAsync(ct);
        var actor = await RequireActorAsync(principal, ct); var target = await RequireTargetAsync(targetId, ct);
        if (!actor.Administrator && (!actor.Roles.Contains(nameof(AccountRole.OrganizationManager))
            || target.Roles.Intersect([nameof(AccountRole.Administrator), nameof(AccountRole.OrganizationManager)]).Any()))
            throw new IdentityFailure("forbidden", 403);
        if (target.DeletedAt is null) throw new IdentityFailure("account_not_restorable", 409);
        if (target.Roles.Contains(nameof(AccountRole.Learner))) await RequireCapacityAsync(actor, transaction, 1, ct);
        var deletedAt = target.DeletedAt; target.DeletedAt = null; target.Status = "active";
        await RecordSecurityChangeAsync(actor, target, "user.restored", new { deletedAt, reason }, ct);
        await transaction.CommitAsync(ct);
    }

    internal async Task ChangeCourseAuthorAsync(ClaimsPrincipal principal, Guid targetId, bool grant, string reason, string? password, CancellationToken ct)
        => await ChangeOrdinaryRoleAsync(principal, targetId, AccountRole.CourseAuthor, grant, reason, password, ct);

    internal async Task ChangeOrdinaryRoleAsync(ClaimsPrincipal principal, Guid targetId, AccountRole role, bool grant, string reason, string? password, CancellationToken ct)
    {
        ValidateReason(reason);
        if (role is AccountRole.Administrator or AccountRole.OrganizationManager || !Enum.IsDefined(role))
            throw new IdentityFailure("role_not_supported", 400);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockOrganizationAsync(ct);
        var actor = await RequireActorAsync(principal, ct);
        var actorRoles = actor.Roles.Select(Enum.Parse<AccountRole>).ToArray();
        if (!RoleGrantPolicy.CanGrantOrRevoke(actorRoles, role)) throw new IdentityFailure("forbidden", 403);
        var target = await RequireTargetAsync(targetId, ct);
        if (!actor.Administrator && target.Id != actor.Id && (target.Administrator || target.Roles.Contains(nameof(AccountRole.OrganizationManager))))
            throw new IdentityFailure("forbidden", 403);
        if (target.Administrator && target.Id != actor.Id) Reauthenticate(actor, password);
        if (!target.Active) throw new IdentityFailure("account_not_active", 409);
        var roleName = role.ToString();
        if (target.Roles.Contains(roleName) == grant) return;
        if (grant && role == AccountRole.Learner) await RequireCapacityAsync(actor, transaction, 1, ct);
        if (!grant)
        {
            var organization = await db.Organizations.AsNoTracking().SingleAsync(x => x.Id == installation.OrganizationId, ct);
            var context = new IdentityWriteContext(new(actor.View(), new(organization.Id, organization.Name)),
                db.Database.GetDbConnection(), transaction.GetDbTransaction());
            foreach (var guard in removalGuards)
                if (await guard.ValidateRoleRemovalAsync(context, target.Id, role, ct) is { } rejection) throw new IdentityFailure(rejection, 409);
        }
        var before = target.Roles;
        target.Roles = grant ? [.. target.Roles, roleName] : target.Roles.Where(x => x != roleName).ToArray();
        var action = role switch
        {
            AccountRole.CourseAuthor => "course_author",
            AccountRole.CohortCoordinator => "cohort_coordinator",
            AccountRole.LearningFacilitator => "learning_facilitator",
            AccountRole.Learner => "learner",
            _ => throw new InvalidOperationException("Unsupported ordinary role.")
        };
        await RecordSecurityChangeAsync(actor, target, grant ? $"{action}.granted" : $"{action}.revoked", new { before, after = target.Roles, reason }, ct);
        await transaction.CommitAsync(ct);
    }

    private async Task LockOrganizationAsync(CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM identity.organizations WHERE id = {installation.OrganizationId} FOR UPDATE", ct);
    }

    private async Task RequireCapacityAsync(UserAccount actor, IDbContextTransaction transaction, int increase, CancellationToken ct)
    {
        var usage = await db.Users.CountAsync(x => x.OrganizationId == installation.OrganizationId && x.Status == "active"
            && x.DeletedAt == null && x.Roles.Contains(nameof(AccountRole.Learner)), ct);
        var organization = await db.Organizations.AsNoTracking().SingleAsync(x => x.Id == installation.OrganizationId, ct);
        var context = new IdentityWriteContext(new(actor.View(), new(organization.Id, organization.Name)),
            db.Database.GetDbConnection(), transaction.GetDbTransaction());
        var decision = await learnerCapacity.ValidateIncreaseAsync(context, usage, increase, ct);
        if (!decision.Allowed) throw new IdentityFailure(decision.Code!, 409);
    }

    private async Task<InvitationRow?> InvitationAsync(Guid userId, bool locked, CancellationToken ct)
    {
        var suffix = locked ? " FOR UPDATE" : "";
        await using var command = new NpgsqlCommand("SELECT user_id,token_hash,expires_at,used_at,version,issued_by,request_id,request_hash FROM identity.invitations WHERE organization_id=$1 AND user_id=$2" + suffix,
            (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.AddWithValue(installation.OrganizationId); command.Parameters.AddWithValue(userId);
        await using var reader = await command.ExecuteReaderAsync(ct); return await ReadInvitationAsync(reader, ct);
    }

    private async Task<InvitationRow?> InvitationByTokenAsync(string hash, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT user_id,token_hash,expires_at,used_at,version,issued_by,request_id,request_hash FROM identity.invitations WHERE organization_id=$1 AND token_hash=$2 FOR UPDATE",
            (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.AddWithValue(installation.OrganizationId); command.Parameters.AddWithValue(hash);
        await using var reader = await command.ExecuteReaderAsync(ct); return await ReadInvitationAsync(reader, ct);
    }

    private async Task<InvitationRow?> InvitationByRequestAsync(Guid requestId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT user_id,token_hash,expires_at,used_at,version,issued_by,request_id,request_hash FROM identity.invitations WHERE organization_id=$1 AND request_id=$2 FOR UPDATE",
            (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.AddWithValue(installation.OrganizationId); command.Parameters.AddWithValue(requestId);
        await using var reader = await command.ExecuteReaderAsync(ct); return await ReadInvitationAsync(reader, ct);
    }

    private static async Task<InvitationRow?> ReadInvitationAsync(NpgsqlDataReader reader, CancellationToken ct) => await reader.ReadAsync(ct)
        ? new(reader.GetGuid(0), reader.GetString(1), reader.GetFieldValue<DateTimeOffset>(2), reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3),
            reader.GetInt32(4), reader.GetGuid(5), reader.GetGuid(6), reader.GetString(7)) : null;

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private sealed record InvitationRow(Guid UserId, string TokenHash, DateTimeOffset ExpiresAt, DateTimeOffset? UsedAt,
        int Version, Guid IssuedBy, Guid RequestId, string RequestHash);

    private async Task<UserAccount> RequireActorAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (principal.FindFirstValue(IdentityClaims.Organization) != installation.OrganizationId.ToString()) throw new IdentityFailure("unauthenticated", 401);
        var id = IdentityClaims.UserId(principal);
        var user = await db.Users.SingleOrDefaultAsync(x => x.OrganizationId == installation.OrganizationId && x.Id == id, ct);
        if (user is null || !user.Active || user.SecurityVersion != IdentityClaims.SecurityVersion(principal)) throw new IdentityFailure("unauthenticated", 401);
        return user;
    }

    private async Task<UserAccount> RequireTargetAsync(Guid id, CancellationToken ct) =>
        await db.Users.SingleOrDefaultAsync(x => x.OrganizationId == installation.OrganizationId && x.Id == id, ct)
        ?? throw new IdentityFailure("user_not_found", 404);

    private void Reauthenticate(UserAccount actor, string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length > 128 || actor.LockoutEnd > clock.GetUtcNow()
            || hasher.VerifyHashedPassword(actor, actor.PasswordHash, password) == PasswordVerificationResult.Failed)
            throw new IdentityFailure("reauthentication_failed", 403);
    }

    private async Task GuardAdministratorContinuityAsync(UserAccount target, CancellationToken ct)
    {
        if (!target.Administrator || !target.Active) return;
        var now = clock.GetUtcNow();
        var another = await db.Users.AnyAsync(x => x.OrganizationId == installation.OrganizationId && x.Id != target.Id
            && x.Status == "active" && x.DeletedAt == null && x.PasswordHash != ""
            && (x.LockoutEnd == null || x.LockoutEnd <= now) && x.Roles.Contains(nameof(AccountRole.Administrator)), ct);
        if (!another) throw new IdentityFailure("final_administrator", 409);
    }

    private async Task RecordSecurityChangeAsync(UserAccount actor, UserAccount target, string action, object metadata, CancellationToken ct)
    {
        target.SecurityVersion++;
        target.UpdatedAt = clock.GetUtcNow();
        db.Audit.Add(new AuditEntry { OrganizationId = installation.OrganizationId, ActorUserId = actor.Id,
            Action = action, TargetId = target.Id, Metadata = JsonSerializer.Serialize(metadata), CreatedAt = clock.GetUtcNow() });
        await db.Sessions.Where(x => x.OrganizationId == installation.OrganizationId && x.UserId == target.Id).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) throw new IdentityFailure("reason_required", 400);
    }
}

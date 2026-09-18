using System.Net;
using System.Net.Mail;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Variable.Identity;
using Variable.Licensing;
using Variable.Notifications;
using Xunit;

namespace Variable.IntegrationTests;

public sealed partial class IdentityTests
{
    [Fact]
    public async Task Utilization_counts_one_mixed_role_active_learner_and_zero_for_pending_staff_or_inactive()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); await LoadLicense(admin, 5, 1);
        var mixed = await Seed("mixed@example.test", ["CourseAuthor", "CohortCoordinator", "LearningFacilitator", "OrganizationManager"]);
        var inactive = await Seed("inactive@example.test", ["Learner"]); await Execute($"UPDATE identity.users SET status='deactivated' WHERE id='{inactive}'");
        await Invite(admin, "pending-count@example.test", ["Learner"]);
        Assert.Equal(0, (await admin.GetFromJsonAsync<LicenseStatusView>("/api/licensing/status"))!.ActiveLearners);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(admin, $"/api/administration/users/{mixed}/learner-role", new { granted = true, reason = "Count mixed role" })).StatusCode);
        var status = await admin.GetFromJsonAsync<LicenseStatusView>("/api/licensing/status");
        Assert.Equal(1, status!.ActiveLearners); Assert.Equal(4, status.Remaining);
    }

    [Fact]
    public async Task Last_seat_acceptance_serializes_and_only_active_learner_accounts_consume_capacity()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin);
        Assert.Equal(HttpStatusCode.OK, (await LoadLicense(admin, 1, 1)).StatusCode);
        var staff = await Seed("staff@example.test", ["CourseAuthor", "LearningFacilitator"]);
        await Execute($"UPDATE identity.users SET status='deactivated',roles=ARRAY['Learner']::text[] WHERE id='{staff}'");
        var first = await Invite(admin, "first@example.test", ["Learner"]);
        var second = await Invite(admin, "second@example.test", ["Learner", "CourseAuthor"]);
        var before = await admin.GetFromJsonAsync<LicenseStatusView>("/api/licensing/status");
        Assert.Equal(0, before!.ActiveLearners);

        var firstToken = await InvitationToken(first.UserId); var secondToken = await InvitationToken(second.UserId);
        var outcomes = await Task.WhenAll(
            Post(Client(), "/api/invitations/accept", new { token = firstToken, password = Password }),
            Post(Client(), "/api/invitations/accept", new { token = secondToken, password = Password }));
        Assert.Single(outcomes, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(outcomes, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.users WHERE status='active' AND deleted_at IS NULL AND 'Learner'=ANY(roles)"));
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.users WHERE status='pending'"));
        var mixed = await Count("SELECT count(*) FROM identity.users WHERE status='active' AND 'Learner'=ANY(roles) AND 'CourseAuthor'=ANY(roles)");
        Assert.InRange(mixed, 0, 1);
    }

    [Fact]
    public async Task Signed_license_enforces_binding_schema_signature_and_monotonic_revisions()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await LoadLicense(admin, 3, 1, organizationId: Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await LoadLicense(admin, 3, 1, schema: 2)).StatusCode);
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        Assert.Equal(HttpStatusCode.BadRequest, (await LoadLicense(admin, 3, 1, signer: other)).StatusCode);
        var validDocument = License(3, 2, includeOptionalEntitlement: true);
        using var publicKey = ECDsa.Create(); publicKey.ImportFromPem(await File.ReadAllTextAsync(licensePublicKey));
        var jose = await new JsonWebTokenHandler().ValidateTokenAsync(validDocument, new TokenValidationParameters
        {
            RequireSignedTokens = true, RequireExpirationTime = false, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new ECDsaSecurityKey(publicKey) { KeyId = "test-issuer", CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false } },
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256], ValidTypes = ["variable-lms-license"],
            ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = false
        });
        Assert.True(jose.IsValid, jose.Exception?.ToString());
        using var valid = await Post(admin, "/api/licensing/license", new { compactJws = validDocument });
        Assert.True(valid.StatusCode == HttpStatusCode.OK, await valid.Content.ReadAsStringAsync());
        var exact = await valid.Content.ReadFromJsonAsync<LicenseStatusView>();
        Assert.Equal(2, exact!.Revision); Assert.Equal(3, exact.MaxActiveLearners);
        Assert.Equal(HttpStatusCode.Conflict, (await LoadLicense(admin, 5, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post(admin, "/api/licensing/license", new { compactJws = validDocument })).StatusCode);
        Assert.Equal(1, await Count("SELECT count(*) FROM licensing.license_documents"));
    }

    [Fact]
    public async Task License_parser_rejects_expiry_unknown_requirements_and_duplicate_properties()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(admin, "/api/licensing/license", new
            { compactJws = License(2, 1, product: "another-product") })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(admin, "/api/licensing/license", new
            { compactJws = License(2, 1, requiredEntitlements: ["max_active_learners", "unknown_feature"]) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(admin, "/api/licensing/license", new
            { compactJws = License(2, 1, keyId: "unknown-issuer") })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(admin, "/api/licensing/license", new
            { compactJws = License(2, 1, expiresAt: clock.GetUtcNow().AddSeconds(-1)) })).StatusCode);
        var now = clock.GetUtcNow();
        var duplicatePayload = $$$"""
            {"schema_version":1,"schema_version":1,"product":"variable-lms","license_id":"duplicate",
             "organization_id":"{{{organization}}}","revision":1,"issued_at":"{{{now.AddMinutes(-1):O}}}",
             "not_before":"{{{now.AddMinutes(-1):O}}}","expires_at":"{{{now.AddDays(1):O}}}",
             "entitlements":{"max_active_learners":2},"required_entitlements":["max_active_learners"],"extensions":{}}
            """;
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(admin, "/api/licensing/license", new
            { compactJws = Signed(duplicatePayload) })).StatusCode);
        Assert.Equal(0, await Count("SELECT count(*) FROM licensing.license_documents"));
    }

    [Fact]
    public async Task Invitation_request_retries_are_idempotent_and_resend_invalidates_the_old_token()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); await LoadLicense(admin, 2, 1);
        var requestId = Guid.NewGuid();
        var first = await Invite(admin, "retry@example.test", ["Learner"], requestId);
        var oldToken = await InvitationToken(first.UserId);
        var retry = await Invite(admin, "retry@example.test", ["Learner"], requestId);
        Assert.Equal(first.UserId, retry.UserId);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(admin, "/api/provisioning/invitations", new
            { requestId, name = "Different payload", email = "different@example.test", roles = new[] { "Learner" } })).StatusCode);
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.users WHERE normalized_email='RETRY@EXAMPLE.TEST'"));
        Assert.Equal(1, await Count("SELECT count(*) FROM notifications.email_intents"));
        await Invite(admin, "retry@example.test", ["Learner"], Guid.NewGuid());
        var currentToken = await InvitationToken(first.UserId);
        Assert.NotEqual(oldToken, currentToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(Client(), "/api/invitations/accept", new { token = oldToken, password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(Client(), "/api/invitations/accept", new { token = currentToken, password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(Client(), "/api/invitations/accept", new { token = currentToken, password = Password })).StatusCode);
        Assert.Equal(2, await Count("SELECT count(*) FROM notifications.email_intents"));
    }

    [Fact]
    public async Task Committed_email_work_survives_and_transient_smtp_failure_retries()
    {
        var transport = new RetryTransport(); await factory.DisposeAsync(); factory = CreateFactory(emailTransport: transport);
        await Bootstrap(); using var admin = Client(); await Login(admin);
        var invitation = await Invite(admin, "delivery@example.test", ["CourseAuthor"]);
        var token = await InvitationToken(invitation.UserId);
        var protectedPayload = await ScalarString("SELECT protected_payload FROM notifications.email_intents");
        Assert.DoesNotContain(token, protectedPayload, StringComparison.Ordinal);
        var dispatcher = factory.Services.GetRequiredService<INotificationDispatcher>();
        Assert.True(await dispatcher.RunOnceAsync()); Assert.Equal(0, transport.Sent);
        Assert.Equal(1, await Count("SELECT count(*) FROM notifications.email_intents WHERE status='pending' AND attempts=1"));
        clock.Advance(TimeSpan.FromSeconds(20));
        Assert.True(await dispatcher.RunOnceAsync()); Assert.Equal(1, transport.Sent);
        Assert.Equal(1, await Count("SELECT count(*) FROM notifications.email_intents WHERE status='sent' AND attempts=2"));
    }

    [Fact]
    public async Task Required_email_intent_failure_rolls_back_pending_account_and_audit()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin);
        await Execute("REVOKE INSERT ON notifications.email_intents FROM variable_runtime");
        var response = await Post(admin, "/api/provisioning/invitations", new
            { requestId = Guid.NewGuid(), name = "Rollback", email = "rollback-invite@example.test", roles = new[] { "CourseAuthor" } });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.users WHERE normalized_email='ROLLBACK-INVITE@EXAMPLE.TEST'"));
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.audit_log WHERE action='user.invited'"));
    }

    [Fact]
    public async Task Learner_grant_reactivation_and_restoration_share_the_capacity_guard()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); await LoadLicense(admin, 1, 1);
        var first = await Seed("grant@example.test", ["CourseAuthor"]);
        var second = await Seed("reactivate@example.test", ["Learner"]);
        var third = await Seed("restore@example.test", ["Learner"]);
        await Execute($"UPDATE identity.users SET status='deactivated' WHERE id='{second}'");
        await Execute($"UPDATE identity.users SET status='deactivated',deleted_at=now() WHERE id='{third}'");
        var outcomes = await Task.WhenAll(
            Post(admin, $"/api/administration/users/{first}/learner-role", new { granted = true, reason = "Capacity race" }),
            Post(admin, $"/api/administration/users/{second}/reactivate", new { reason = "Capacity race" }),
            Post(admin, $"/api/administration/users/{third}/restore", new { reason = "Capacity race" }));
        Assert.Single(outcomes, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Equal(2, outcomes.Count(x => x.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.users WHERE status='active' AND deleted_at IS NULL AND 'Learner'=ANY(roles)"));
        Assert.Equal(HttpStatusCode.Conflict, (await LoadLicense(admin, 0, 2)).StatusCode);
        Assert.Equal(1, (await admin.GetFromJsonAsync<LicenseStatusView>("/api/licensing/status"))!.MaxActiveLearners);
    }

    [Fact]
    public async Task License_replacement_races_active_learner_grant_under_the_same_guard()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); await LoadLicense(admin, 1, 1);
        var target = await Seed("replacement-race@example.test", ["CourseAuthor"]);
        var outcomes = await Task.WhenAll(
            Post(admin, $"/api/administration/users/{target}/learner-role", new { granted = true, reason = "Replacement race" }),
            LoadLicense(admin, 0, 2));
        Assert.Single(outcomes, x => x.StatusCode == HttpStatusCode.NoContent || x.StatusCode == HttpStatusCode.OK);
        Assert.Single(outcomes, x => x.StatusCode == HttpStatusCode.Conflict);
        var status = await admin.GetFromJsonAsync<LicenseStatusView>("/api/licensing/status");
        Assert.True((status!.MaxActiveLearners == 0 && status.ActiveLearners == 0)
            || (status.MaxActiveLearners == 1 && status.ActiveLearners == 1));
    }

    [Fact]
    public async Task Managers_cannot_escalate_invitation_roles_and_deactivation_releases_a_seat()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); await LoadLicense(admin, 1, 1);
        await Seed("manager-invite@example.test", ["OrganizationManager"]);
        using var manager = Client(); await Login(manager, "manager-invite@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(manager, "/api/provisioning/invitations", new
            { requestId = Guid.NewGuid(), name = "Escalation", email = "escalation@example.test", roles = new[] { "OrganizationManager" } })).StatusCode);
        var invitation = await Invite(admin, "learner-release@example.test", ["Learner"]);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(Client(), "/api/invitations/accept", new { token = await InvitationToken(invitation.UserId), password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(manager, $"/api/administration/users/{invitation.UserId}/deactivate", new { reason = "Release seat" })).StatusCode);
        var replacement = await Invite(manager, "replacement@example.test", ["Learner"]);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(Client(), "/api/invitations/accept", new { token = await InvitationToken(replacement.UserId), password = Password })).StatusCode);
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.users WHERE status='active' AND 'Learner'=ANY(roles)"));
        Assert.Equal(3, await Count("SELECT count(*) FROM identity.audit_log WHERE action IN ('invitation.accepted','user.deactivated')"));
    }

    private async Task<InvitationView> Invite(HttpClient client, string email, string[] roles, Guid? requestId = null)
    {
        using var response = await Post(client, "/api/provisioning/invitations", new
            { requestId = requestId ?? Guid.NewGuid(), name = "Synthetic Invitee", email, roles });
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<InvitationView>())!;
    }

    private Task<HttpResponseMessage> LoadLicense(HttpClient client, int maximum, long revision, Guid? organizationId = null,
        int schema = 1, ECDsa? signer = null) => Post(client, "/api/licensing/license",
            new { compactJws = License(maximum, revision, organizationId, schema, signer) });

    private string License(int maximum, long revision, Guid? organizationId = null, int schema = 1, ECDsa? signer = null,
        string product = "variable-lms", string[]? requiredEntitlements = null, DateTimeOffset? expiresAt = null,
        string keyId = "test-issuer", bool includeOptionalEntitlement = false)
    {
        var entitlements = new Dictionary<string, object> { ["max_active_learners"] = maximum };
        if (includeOptionalEntitlement) entitlements["future_optional_display"] = "ignored";
        var payload = JsonSerializer.Serialize(new
        {
            schema_version = schema, product, license_id = $"test-license-{revision}",
            organization_id = (organizationId ?? organization).ToString(), revision,
            issued_at = clock.GetUtcNow().AddMinutes(-1).ToString("O"), not_before = clock.GetUtcNow().AddMinutes(-1).ToString("O"),
            expires_at = (expiresAt ?? clock.GetUtcNow().AddDays(30)).ToString("O"), entitlements,
            required_entitlements = requiredEntitlements ?? ["max_active_learners"], extensions = new { fixture = true }
        });
        return Signed(payload, signer, keyId);
    }

    private string Signed(string payload, ECDsa? signer = null, string keyId = "test-issuer")
    {
        var key = new ECDsaSecurityKey(signer ?? licenseIssuer) { KeyId = keyId, CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false } };
        return new JsonWebTokenHandler().CreateToken(payload, new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256),
            new Dictionary<string, object> { ["typ"] = "variable-lms-license" });
    }

    private async Task<string> InvitationToken(Guid userId)
    {
        await using var connection = new NpgsqlConnection(OwnerConnection); await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT protected_payload FROM notifications.email_intents WHERE recipient_user_id=$1 ORDER BY dedupe_key DESC LIMIT 1", connection);
        command.Parameters.AddWithValue(userId); var protectedPayload = (string)(await command.ExecuteScalarAsync())!;
        var provider = factory.Services.GetRequiredService<IDataProtectionProvider>();
        var json = provider.CreateProtector("Variable.LMS.Notification.Invitation.v1").Unprotect(protectedPayload);
        return JsonDocument.Parse(json).RootElement.GetProperty("Token").GetString()!;
    }

    private async Task<string> ScalarString(string sql)
    {
        await using var connection = new NpgsqlConnection(OwnerConnection); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection); return (string)(await command.ExecuteScalarAsync())!;
    }

    private sealed class RetryTransport : IEmailTransport
    {
        private int attempts;
        internal int Sent { get; private set; }
        public Task SendAsync(EmailMessage message, CancellationToken ct)
        {
            if (Interlocked.Increment(ref attempts) == 1) throw new SmtpException("Synthetic transient failure");
            Sent++; return Task.CompletedTask;
        }
    }
}

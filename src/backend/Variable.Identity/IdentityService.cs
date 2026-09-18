using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Variable.Identity.Persistence;
using Variable.Database;

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

internal sealed class IdentityService(IdentityDb db, InstallationOptions installation, IPasswordHasher<UserAccount> hasher, TimeProvider clock, MigrationPlan migrations)
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
        var before = target.Status;
        target.Status = "deactivated";
        await RecordSecurityChangeAsync(actor, target, "user.deactivated", new { before, after = target.Status, reason }, ct);
        await transaction.CommitAsync(ct);
    }

    internal async Task<AccountView[]> FindAccountsAsync(ClaimsPrincipal principal, string search, CancellationToken ct)
    {
        var actor = await RequireActorAsync(principal, ct);
        if (!actor.Administrator && !actor.Roles.Contains(nameof(AccountRole.OrganizationManager))) throw new IdentityFailure("forbidden", 403);
        return (await db.Users.AsNoTracking().Where(x => x.OrganizationId == installation.OrganizationId && x.Status == "active" && x.DeletedAt == null
            && x.NormalizedEmail.Contains(search.ToUpperInvariant())).OrderBy(x => x.NormalizedEmail).ThenBy(x => x.Id).Take(50).ToListAsync(ct)).Select(x => x.View()).ToArray();
    }

    internal async Task ChangeCourseAuthorAsync(ClaimsPrincipal principal, Guid targetId, bool grant, string reason, string? password, CancellationToken ct)
    {
        ValidateReason(reason);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockOrganizationAsync(ct);
        var actor = await RequireActorAsync(principal, ct);
        var actorRoles = actor.Roles.Select(Enum.Parse<AccountRole>).ToArray();
        if (!RoleGrantPolicy.CanGrantOrRevoke(actorRoles, AccountRole.CourseAuthor)) throw new IdentityFailure("forbidden", 403);
        var target = await RequireTargetAsync(targetId, ct);
        if (!actor.Administrator && target.Id != actor.Id && (target.Administrator || target.Roles.Contains(nameof(AccountRole.OrganizationManager))))
            throw new IdentityFailure("forbidden", 403);
        if (target.Administrator && target.Id != actor.Id) Reauthenticate(actor, password);
        if (!target.Active) throw new IdentityFailure("account_not_active", 409);
        if (target.Roles.Contains(nameof(AccountRole.CourseAuthor)) == grant) return;
        var before = target.Roles;
        target.Roles = grant ? [.. target.Roles, nameof(AccountRole.CourseAuthor)] : target.Roles.Where(x => x != nameof(AccountRole.CourseAuthor)).ToArray();
        await RecordSecurityChangeAsync(actor, target, grant ? "course_author.granted" : "course_author.revoked", new { before, after = target.Roles, reason }, ct);
        await transaction.CommitAsync(ct);
    }

    private async Task LockOrganizationAsync(CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM identity.organizations WHERE id = {installation.OrganizationId} FOR UPDATE", ct);
    }

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

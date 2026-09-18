using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Variable.Identity.Persistence;

namespace Variable.Identity;

public interface IIdentityAccess
{
    Task<SessionView> CurrentAsync(ClaimsPrincipal principal, CancellationToken ct);
    Task<IdentityWork> BeginWriteAsync(ClaimsPrincipal principal, CancellationToken ct);
    Task<AccountView[]> EligibleOwnersAsync(ClaimsPrincipal principal, string search, CancellationToken ct);
    Task<AccountView[]> EligibleCohortStaffAsync(ClaimsPrincipal principal, string capability, string search, CancellationToken ct);
}

internal sealed class IdentityAccess(IdentityDb db, IdentityService identity, InstallationOptions installation, TimeProvider clock) : IIdentityAccess
{
    public Task<SessionView> CurrentAsync(ClaimsPrincipal principal, CancellationToken ct) => identity.CurrentAsync(principal, ct);

    public async Task<IdentityWork> BeginWriteAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Shares the guard used by role changes/deactivation; authority stays current through commit.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM identity.organizations WHERE id = {installation.OrganizationId} FOR UPDATE", ct);
            var actor = await identity.CurrentAsync(principal, ct);
            return new IdentityWork(db, transaction, actor, clock);
        }
        catch { await transaction.DisposeAsync(); throw; }
    }

    public async Task<AccountView[]> EligibleOwnersAsync(ClaimsPrincipal principal, string search, CancellationToken ct)
    {
        var actor = await identity.CurrentAsync(principal, ct);
        var administrator = actor.Account.Roles.Contains(nameof(AccountRole.Administrator));
        if (!administrator && !actor.Account.Roles.Intersect([nameof(AccountRole.CourseAuthor), nameof(AccountRole.OrganizationManager)]).Any()) throw new IdentityFailure("forbidden", 403);
        var query = db.Users.AsNoTracking().Where(x => x.OrganizationId == installation.OrganizationId && x.Status == "active" && x.DeletedAt == null
            && (x.Roles.Contains(nameof(AccountRole.CourseAuthor)) || x.Roles.Contains(nameof(AccountRole.Administrator))));
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.NormalizedEmail.Contains(search.ToUpperInvariant()));
        return (await query.OrderBy(x => x.NormalizedEmail).ThenBy(x => x.Id).Take(50).ToListAsync(ct)).Select(x => x.View()).ToArray();
    }

    public async Task<AccountView[]> EligibleCohortStaffAsync(ClaimsPrincipal principal, string capability, string search, CancellationToken ct)
    {
        var actor = await identity.CurrentAsync(principal, ct);
        if (!actor.Account.Roles.Intersect([nameof(AccountRole.Administrator), nameof(AccountRole.OrganizationManager), nameof(AccountRole.CohortCoordinator)]).Any())
            throw new IdentityFailure("forbidden", 403);
        var requiredRole = capability switch
        {
            "coordinator" => nameof(AccountRole.CohortCoordinator),
            "facilitator" => nameof(AccountRole.LearningFacilitator),
            _ => throw new IdentityFailure("invalid_staff_capability", 400)
        };
        var query = db.Users.AsNoTracking().Where(x => x.OrganizationId == installation.OrganizationId && x.Status == "active"
            && x.DeletedAt == null && x.Roles.Contains(requiredRole));
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.NormalizedEmail.Contains(search.ToUpperInvariant()));
        return (await query.OrderBy(x => x.NormalizedEmail).ThenBy(x => x.Id).Take(50).ToListAsync(ct)).Select(x => x.View()).ToArray();
    }
}

// A local transaction contract, not a domain entity or a distributed unit of work.
// Consumers mutate only their own schemas; Identity owns eligibility and audit operations.
public sealed class IdentityWork : IAsyncDisposable
{
    private readonly IdentityDb db;
    private readonly IDbContextTransaction transaction;
    private readonly TimeProvider clock;
    internal IdentityWork(IdentityDb db, IDbContextTransaction transaction, SessionView actor, TimeProvider clock)
    { this.db = db; this.transaction = transaction; Actor = actor; this.clock = clock; }
    public SessionView Actor { get; }
    public DbConnection Connection => db.Database.GetDbConnection();
    public DbTransaction Transaction => transaction.GetDbTransaction();

    public async Task RequireEligibleOwnerAsync(Guid id, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.OrganizationId == Actor.Organization.Id && x.Id == id
            && x.Status == "active" && x.DeletedAt == null && (x.Roles.Contains(nameof(AccountRole.CourseAuthor)) || x.Roles.Contains(nameof(AccountRole.Administrator))), ct))
            throw new IdentityFailure("owner_not_eligible", 400);
    }

    public async Task<AccountView> RequireEligibleCohortStaffAsync(Guid id, string capability, CancellationToken ct)
    {
        var requiredRole = capability switch
        {
            "coordinator" => nameof(AccountRole.CohortCoordinator),
            "facilitator" => nameof(AccountRole.LearningFacilitator),
            _ => throw new IdentityFailure("invalid_staff_capability", 400)
        };
        var user = await db.Users.SingleOrDefaultAsync(x => x.OrganizationId == Actor.Organization.Id && x.Id == id
            && x.Status == "active" && x.DeletedAt == null && x.Roles.Contains(requiredRole), ct);
        return user?.View() ?? throw new IdentityFailure("staff_not_eligible", 400);
    }

    public async Task AuditAsync(string action, Guid target, object metadata, CancellationToken ct)
    {
        db.Audit.Add(new AuditEntry { OrganizationId = Actor.Organization.Id, ActorUserId = Actor.Account.Id,
            Action = action, TargetId = target, Metadata = JsonSerializer.Serialize(metadata), CreatedAt = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct);
    }

    public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}

// Read-only transaction handle for invariant guards. It deliberately cannot commit,
// audit, or mutate Identity state.
public sealed class IdentityWriteContext
{
    internal IdentityWriteContext(SessionView actor, DbConnection connection, DbTransaction transaction)
    { Actor = actor; Connection = connection; Transaction = transaction; }
    public SessionView Actor { get; }
    public DbConnection Connection { get; }
    public DbTransaction Transaction { get; }
}

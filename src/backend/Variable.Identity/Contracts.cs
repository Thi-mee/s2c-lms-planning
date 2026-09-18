namespace Variable.Identity;

public enum AccountRole
{
    Learner, CourseAuthor, CohortCoordinator, LearningFacilitator, OrganizationManager, Administrator
}

public sealed record AccountView(Guid Id, string Name, string Email, string[] Roles);
public sealed record OrganizationView(Guid Id, string Name);
public sealed record SessionView(AccountView Account, OrganizationView Organization);
public sealed record ProvisioningAccountView(Guid Id, string Name, string Email, string Status, string[] Roles, DateTimeOffset? InvitationExpiresAt);
public sealed record InviteAccount(Guid RequestId, string Name, string Email, AccountRole[] Roles);
public sealed record InvitationView(Guid UserId, string Status, DateTimeOffset ExpiresAt);

public sealed record LearnerCapacityDecision(bool Allowed, string? Code)
{
    public static LearnerCapacityDecision Permit { get; } = new(true, null);
    public static LearnerCapacityDecision Reject(string code) => new(false, code);
}

// Licensing evaluates only positive consumption deltas on Identity's already-locked
// local transaction. Existing-account lifecycle behavior remains gated by Q68.
public interface ILearnerCapacityPolicy
{
    Task<LearnerCapacityDecision> ValidateIncreaseAsync(IdentityWriteContext context, int currentUsage, int increase, CancellationToken ct);
}

// Notifications owns its table and protected delivery payload. Identity supplies an
// already-open transaction so invite state and its required email intent commit together.
public interface IInvitationEmailWriter
{
    Task EnqueueAsync(IdentityWriteContext context, Guid userId, string email, string token,
        DateTimeOffset expiresAt, int invitationVersion, CancellationToken ct);
}

public static class RoleGrantPolicy
{
    public static bool CanGrantOrRevoke(IReadOnlyCollection<AccountRole> actorRoles, AccountRole targetRole)
    {
        if (!Enum.IsDefined(targetRole) || targetRole == AccountRole.Administrator) return false;
        if (actorRoles.Contains(AccountRole.Administrator)) return true;
        return actorRoles.Contains(AccountRole.OrganizationManager) && targetRole is
            AccountRole.Learner or AccountRole.CourseAuthor or AccountRole.CohortCoordinator or AccountRole.LearningFacilitator;
    }
}

// Resource-owning modules implement this hook when removing an account capability can
// invalidate one of their invariants. Validation joins the Identity transaction.
public interface IAccountAccessRemovalGuard
{
    Task<string?> ValidateRoleRemovalAsync(IdentityWriteContext context, Guid userId, AccountRole role, CancellationToken ct);
    Task<string?> ValidateDeactivationAsync(IdentityWriteContext context, Guid userId, CancellationToken ct);
}

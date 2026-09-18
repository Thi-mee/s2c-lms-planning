namespace Variable.Identity;

public enum AccountRole
{
    Learner, CourseAuthor, CohortCoordinator, LearningFacilitator, OrganizationManager, Administrator
}

public sealed record AccountView(Guid Id, string Name, string Email, string[] Roles);
public sealed record OrganizationView(Guid Id, string Name);
public sealed record SessionView(AccountView Account, OrganizationView Organization);

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

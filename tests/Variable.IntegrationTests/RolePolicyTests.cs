using Variable.Identity;
using Variable.Authoring;
using Variable.Database;
using Variable.Enrollment;
using Xunit;

namespace Variable.IntegrationTests;

public sealed class RolePolicyTests
{
    public static IEnumerable<object[]> Matrix()
    {
        foreach (var actor in Enum.GetValues<AccountRole>())
        foreach (var target in Enum.GetValues<AccountRole>())
        {
            var expected = target != AccountRole.Administrator && (actor == AccountRole.Administrator
                || (actor == AccountRole.OrganizationManager && target is not AccountRole.OrganizationManager));
            yield return [actor, target, expected];
        }
    }

    [Theory, MemberData(nameof(Matrix))]
    public void Ordinary_role_grants_use_the_explicit_matrix(AccountRole actor, AccountRole target, bool expected) =>
        Assert.Equal(expected, RoleGrantPolicy.CanGrantOrRevoke([actor], target));

    [Fact]
    public void Combining_operational_roles_never_confers_security_delegation()
    {
        AccountRole[] roles = [AccountRole.CourseAuthor, AccountRole.CohortCoordinator, AccountRole.LearningFacilitator, AccountRole.Learner];
        Assert.All(Enum.GetValues<AccountRole>(), target => Assert.False(RoleGrantPolicy.CanGrantOrRevoke(roles, target)));
        Assert.False(RoleGrantPolicy.CanGrantOrRevoke([AccountRole.Administrator], (AccountRole)999));
    }

    [Fact]
    public void Module_implementation_is_not_a_public_dependency_surface()
    {
        var assembly = typeof(RoleGrantPolicy).Assembly;
        Assert.DoesNotContain(assembly.GetExportedTypes(), type => type.Namespace?.Contains("Persistence", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), referenced => referenced.Name == "Variable.App");
        Assert.All(assembly.GetTypes().Where(type => type.Name is "IdentityDb" or "IdentityService" or "UserAccount" or "PostgresTicketStore"), type => Assert.False(type.IsPublic));
        var authoring = typeof(AuthoringModule).Assembly;
        Assert.DoesNotContain(authoring.GetExportedTypes(), type => type.Name == "AuthoringService" || type.Namespace?.Contains("Persistence", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name == "Variable.Authoring");
        Assert.DoesNotContain(authoring.GetReferencedAssemblies(), reference => reference.Name == "Variable.App");
        var enrollment = typeof(EnrollmentModule).Assembly;
        Assert.DoesNotContain(enrollment.GetExportedTypes(), type => type.Name is "EnrollmentService" or "CohortAccessRemovalGuard" || type.Namespace?.Contains("Persistence", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(enrollment.GetReferencedAssemblies(), reference => reference.Name == "Variable.App");
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name == "Variable.Enrollment");
        Assert.DoesNotContain(authoring.GetReferencedAssemblies(), reference => reference.Name == "Variable.Enrollment");
        Assert.DoesNotContain(typeof(MigrationPlan).Assembly.GetReferencedAssemblies(), reference => reference.Name is "Variable.Identity" or "Variable.Authoring" or "Variable.Enrollment" or "Variable.App");
    }
}

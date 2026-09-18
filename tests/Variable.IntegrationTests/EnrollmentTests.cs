using System.Net;
using System.Net.Http.Json;
using Npgsql;
using Variable.App;
using Variable.Authoring;
using Variable.Database;
using Variable.Enrollment;
using Variable.Identity;
using Xunit;

namespace Variable.IntegrationTests;

public sealed partial class IdentityTests
{
    private async Task<CourseDetail> PublishedCourse(HttpClient client, Guid owner)
    {
        var course = await Create(client, owner);
        var path = $"/api/authoring/courses/{course.Course.Id}";
        (await Post(client, path + "/draft", ValidDraft())).EnsureSuccessStatusCode();
        (await Post(client, path + "/publish", new PublishCourse(2))).EnsureSuccessStatusCode();
        return (await client.GetFromJsonAsync<CourseDetail>(path))!;
    }

    private async Task<CohortSummary> CreateCohort(HttpClient client, Guid courseId, Guid coordinatorId,
        Guid? facilitatorId = null, Guid? id = null, DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        var response = await Post(client, "/api/cohorts", new CreateCohort(id ?? Guid.NewGuid(), courseId, "Synthetic cohort",
            start ?? clock.GetUtcNow().AddHours(-1), end ?? clock.GetUtcNow().AddDays(7), coordinatorId, facilitatorId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CohortSummary>())!;
    }

    [Fact]
    public async Task Cohort_is_created_with_published_course_and_atomic_distinct_staff_capabilities()
    {
        await Bootstrap(); var staff = await Seed("staff@example.test", ["CohortCoordinator", "LearningFacilitator"]);
        using var admin = Client(); await Login(admin); var course = await PublishedCourse(admin, await UserId(admin));
        var cohortId = Guid.NewGuid();
        var cohort = await CreateCohort(admin, course.Course.Id, staff, staff, cohortId);
        var retry = await CreateCohort(admin, course.Course.Id, staff, staff, cohortId);
        Assert.Equal(cohort.Id, retry.Id); Assert.Equal(1, await Count("SELECT count(*) FROM enrollment.cohorts"));
        Assert.Equal("active", cohort.Lifecycle); Assert.Equal(TimeSpan.Zero, cohort.StartAt.Offset); Assert.Equal(TimeSpan.Zero, cohort.EndAt.Offset);
        Assert.Equal(2, cohort.Staff.Length); Assert.All(cohort.Staff, assignment => Assert.True(assignment.Effective));
        Assert.Contains(cohort.Staff, x => x.Capability == "coordinator"); Assert.Contains(cohort.Staff, x => x.Capability == "facilitator");
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.audit_log WHERE action='cohort.created'"));
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.users WHERE id IN (SELECT user_id FROM enrollment.cohort_staff) AND 'Administrator'=ANY(roles)"));
    }

    [Fact]
    public async Task Staff_assignment_never_grants_account_role_and_only_matching_eligible_accounts_are_accepted()
    {
        await Bootstrap(); var manager = await Seed("manager@example.test", ["OrganizationManager"]);
        var coordinator = await Seed("coordinator@example.test", ["CohortCoordinator"]);
        using var admin = Client(); await Login(admin); var course = await PublishedCourse(admin, await UserId(admin));
        using var managerClient = Client(); await Login(managerClient, "manager@example.test");
        var rejected = await Post(managerClient, "/api/cohorts", new CreateCohort(Guid.NewGuid(), course.Course.Id, "Invalid staff",
            clock.GetUtcNow(), clock.GetUtcNow().AddDays(1), coordinator, coordinator));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode); Assert.Contains("staff_not_eligible", await rejected.Content.ReadAsStringAsync());
        Assert.DoesNotContain("LearningFacilitator", (await admin.GetFromJsonAsync<AccountView[]>("/api/administration/users?search=coordinator"))!.Single().Roles);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(managerClient, $"/api/administration/users/{coordinator}/learning-facilitator-role",
            new { granted = true, reason = "Support the cohort" })).StatusCode);
        var cohort = await CreateCohort(managerClient, course.Course.Id, coordinator, coordinator);
        Assert.Equal(2, cohort.Staff.Length);
    }

    [Fact]
    public async Task Coordinator_scope_is_current_and_facilitator_or_author_alone_cannot_manage_cohorts()
    {
        await Bootstrap(); var assigned = await Seed("assigned@example.test", ["CohortCoordinator"]);
        await Seed("other@example.test", ["CohortCoordinator"]); var facilitatorId = await Seed("facilitator@example.test", ["LearningFacilitator"]);
        await Seed("author@example.test", ["CourseAuthor"]);
        using var admin = Client(); await Login(admin); var course = await PublishedCourse(admin, await UserId(admin));
        var cohort = await CreateCohort(admin, course.Course.Id, assigned, facilitatorId);
        using var assignedClient = Client(); await Login(assignedClient, "assigned@example.test");
        Assert.Single((await assignedClient.GetFromJsonAsync<CohortSummary[]>("/api/cohorts"))!);
        using var other = Client(); await Login(other, "other@example.test");
        Assert.Empty((await other.GetFromJsonAsync<CohortSummary[]>("/api/cohorts"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/cohorts/{cohort.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(other, $"/api/cohorts/{cohort.Id}/staff",
            new ChangeCohortStaff(1, await UserId(other), "coordinator", true, "Guess another cohort"))).StatusCode);
        using var facilitator = Client(); await Login(facilitator, "facilitator@example.test");
        Assert.Equal(HttpStatusCode.OK, (await facilitator.GetAsync($"/api/cohorts/{cohort.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(facilitator, $"/api/cohorts/{cohort.Id}/staff",
            new ChangeCohortStaff(1, facilitatorId, "facilitator", false, "Not a coordinator"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(facilitator, "/api/cohorts", new CreateCohort(Guid.NewGuid(), course.Course.Id,
            "No", clock.GetUtcNow(), clock.GetUtcNow().AddDays(1), assigned))).StatusCode);
        using var author = Client(); await Login(author, "author@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await author.GetAsync("/api/cohorts")).StatusCode);
    }

    [Fact]
    public async Task Scheduled_cohort_cannot_lose_its_last_effective_coordinator_by_grant_role_or_account_change()
    {
        await Bootstrap(); var first = await Seed("first@example.test", ["CohortCoordinator"]);
        var second = await Seed("second@example.test", ["CohortCoordinator"]);
        using var admin = Client(); await Login(admin); var course = await PublishedCourse(admin, await UserId(admin));
        var cohort = await CreateCohort(admin, course.Course.Id, first);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(admin, $"/api/cohorts/{cohort.Id}/staff",
            new ChangeCohortStaff(1, first, "coordinator", false, "Remove only coordinator"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(admin, $"/api/administration/users/{first}/cohort-coordinator-role",
            new { granted = false, reason = "Remove role" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(admin, $"/api/administration/users/{first}/deactivate",
            new { reason = "Deactivate only coordinator" })).StatusCode);
        cohort = (await (await Post(admin, $"/api/cohorts/{cohort.Id}/staff", new ChangeCohortStaff(1, second, "coordinator", true, "Add coverage")))
            .Content.ReadFromJsonAsync<CohortSummary>())!;
        var races = await Task.WhenAll(
            Post(admin, $"/api/cohorts/{cohort.Id}/staff", new ChangeCohortStaff(2, first, "coordinator", false, "Rotate coverage")),
            Post(admin, $"/api/cohorts/{cohort.Id}/staff", new ChangeCohortStaff(2, second, "coordinator", false, "Rotate coverage")));
        Assert.Single(races, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(races, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, await Count($"SELECT count(*) FROM enrollment.cohort_staff WHERE cohort_id='{cohort.Id}' AND capability='coordinator'"));
    }

    [Fact]
    public async Task Lifecycle_uses_injected_utc_clock_without_choosing_boundary_access_policy()
    {
        await Bootstrap(); var coordinator = await Seed("coordinator@example.test", ["CohortCoordinator"]);
        using var admin = Client(); await Login(admin); var course = await PublishedCourse(admin, await UserId(admin));
        var cohort = await CreateCohort(admin, course.Course.Id, coordinator, start: clock.GetUtcNow().AddHours(1), end: clock.GetUtcNow().AddHours(2));
        Assert.Equal("upcoming", cohort.Lifecycle);
        clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal("active", (await admin.GetFromJsonAsync<CohortSummary>($"/api/cohorts/{cohort.Id}"))!.Lifecycle);
        clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal("ended", (await admin.GetFromJsonAsync<CohortSummary>($"/api/cohorts/{cohort.Id}"))!.Lifecycle);
    }

    [Fact]
    public async Task Cohort_write_rejects_cross_organization_ids_payload_injection_and_audit_failure_rolls_back()
    {
        await Bootstrap(); var coordinator = await Seed("coordinator@example.test", ["CohortCoordinator"]);
        var foreignOrganization = Guid.NewGuid();
        var foreign = await Seed("foreign@example.test", ["CohortCoordinator", "CourseAuthor"], foreignOrganization);
        using var admin = Client(); await Login(admin); var course = await PublishedCourse(admin, await UserId(admin));
        var wrongStaff = await Post(admin, "/api/cohorts", new CreateCohort(Guid.NewGuid(), course.Course.Id, "Foreign",
            clock.GetUtcNow(), clock.GetUtcNow().AddDays(1), foreign));
        Assert.Equal(HttpStatusCode.BadRequest, wrongStaff.StatusCode);
        var foreignCourse = Guid.NewGuid();
        await Execute($"""
            BEGIN;
            INSERT INTO authoring.courses(organization_id,id,title,description,status,created_by,create_hash,created_at,updated_at,published_at)
            VALUES ('{foreignOrganization}','{foreignCourse}','Foreign published course','','published','{foreign}','fixture',now(),now(),now());
            INSERT INTO authoring.course_authors(id,organization_id,course_id,user_id,created_at)
            VALUES ('{Guid.NewGuid()}','{foreignOrganization}','{foreignCourse}','{foreign}',now());
            COMMIT;
            """);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(admin, "/api/cohorts", new CreateCohort(Guid.NewGuid(), foreignCourse, "Foreign course",
            clock.GetUtcNow(), clock.GetUtcNow().AddDays(1), coordinator))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(admin, "/api/cohorts", new { id = Guid.NewGuid(), courseId = course.Course.Id,
            title = "Injected", startAt = clock.GetUtcNow(), endAt = clock.GetUtcNow().AddDays(1), coordinatorId = coordinator, organizationId = Guid.NewGuid() })).StatusCode);
        await Execute("REVOKE INSERT ON identity.audit_log FROM variable_runtime");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Post(admin, "/api/cohorts", new CreateCohort(Guid.NewGuid(), course.Course.Id, "Rollback",
            clock.GetUtcNow(), clock.GetUtcNow().AddDays(1), coordinator))).StatusCode);
        Assert.Equal(0, await Count("SELECT count(*) FROM enrollment.cohorts"));
    }

    [Fact]
    public async Task Runtime_cannot_delete_cohort_history_and_v2_upgrade_preserves_existing_course()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); var course = await PublishedCourse(admin, await UserId(admin));
        await Execute("DROP TABLE identity.invitations; DROP SCHEMA notifications CASCADE; DROP SCHEMA licensing CASCADE; DROP SCHEMA enrollment CASCADE; DELETE FROM platform.schema_migrations WHERE version>=3");
        var old = new MigrationPlan(IdentityModule.Migration, AuthoringModule.Migration);
        Assert.True(await old.ReadyAsync(RuntimeConnection));
        await ApplicationMigrations.Plan.ApplyAsync(MigrationConnection, "variable_runtime");
        Assert.Equal(1, await Count($"SELECT count(*) FROM authoring.courses WHERE id='{course.Course.Id}'"));
        await using var connection = new NpgsqlConnection(RuntimeConnection); await connection.OpenAsync();
        await using var delete = new NpgsqlCommand("DELETE FROM enrollment.cohorts", connection);
        Assert.Equal("42501", (await Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync())).SqlState);
        Assert.False(await old.ReadyAsync(RuntimeConnection));
    }
}

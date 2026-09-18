using System.Net;
using System.Net.Http.Json;
using Npgsql;
using Variable.App;
using Variable.Authoring;
using Variable.Database;
using Variable.Identity;
using Xunit;

namespace Variable.IntegrationTests;

public sealed partial class IdentityTests
{
    private static SaveCourse ValidDraft(long revision = 1, string notes = "A useful first lesson.", bool required = true) =>
        new(revision, "Synthetic course", "Training description", [new(Guid.NewGuid(), "Welcome", [new(Guid.NewGuid(), "Getting started", notes, required)])]);

    private static async Task<CourseDetail> Create(HttpClient client, Guid owner, Guid? id = null)
    {
        var response = await Post(client, "/api/authoring/courses", new CreateCourse(id ?? Guid.NewGuid(), "Synthetic course", "Training description", owner));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseDetail>())!;
    }

    [Fact]
    public async Task Author_creates_saves_and_publishes_with_an_atomic_owner_and_required_content()
    {
        await Bootstrap(); var authorId = await Seed("author@example.test", ["CourseAuthor"]);
        using var author = Client(); await Login(author, "author@example.test");
        var course = await Create(author, authorId); var path = $"/api/authoring/courses/{course.Course.Id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(author, path + "/publish", new PublishCourse(1))).StatusCode);
        var draft = ValidDraft();
        Assert.Equal(HttpStatusCode.OK, (await Post(author, path + "/draft", draft)).StatusCode);
        var responses = await Task.WhenAll(Post(author, path + "/publish", new PublishCourse(2)), Post(author, path + "/publish", new PublishCourse(2)));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var published = (await author.GetFromJsonAsync<CourseDetail>(path))!;
        Assert.Equal("published", published.Course.Status); Assert.Equal(3, published.Course.Revision);
        Assert.Equal(authorId, published.Course.OwnerId); Assert.Equal(draft.Modules[0].Lessons[0].Id, published.Modules[0].Lessons[0].Id);
        Assert.Equal(1, await Count("SELECT count(*) FROM authoring.course_authors"));
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.audit_log WHERE action = 'course.published'"));
        Assert.Equal(HttpStatusCode.Conflict, (await Post(author, path + "/draft", draft with { ExpectedRevision = 3 })).StatusCode);
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.users WHERE 'Learner'=ANY(roles)"));
    }

    [Fact]
    public async Task Authoring_enforces_current_role_owner_and_organization_for_reads_and_writes()
    {
        await Bootstrap(); var owner = await Seed("owner@example.test", ["CourseAuthor"]);
        var other = await Seed("other@example.test", ["CourseAuthor"]);
        var foreign = await Seed("foreign@example.test", ["CourseAuthor"], Guid.NewGuid());
        using var admin = Client(); await Login(admin); var course = await Create(admin, owner);
        using var author = Client(); await Login(author, "other@example.test"); var path = $"/api/authoring/courses/{course.Course.Id}";
        Assert.Empty((await author.GetFromJsonAsync<CourseSummary[]>("/api/authoring/courses"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await author.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(author, path + "/draft", ValidDraft())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(author, "/api/authoring/courses", new CreateCourse(Guid.NewGuid(), "Stolen", "", owner))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(admin, "/api/authoring/courses", new CreateCourse(Guid.NewGuid(), "Foreign", "", foreign))).StatusCode);
        using var learner = Client(); await Seed("learner@example.test", ["Learner"]); await Login(learner, "learner@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await learner.GetAsync("/api/authoring/courses")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(learner, "/api/authoring/courses", new CreateCourse(Guid.NewGuid(), "No", "", other))).StatusCode);
    }

    [Fact]
    public async Task Manager_grants_author_without_privilege_escalation_and_role_revocation_blocks_existing_owner()
    {
        await Bootstrap(); var managerId = await Seed("manager@example.test", ["OrganizationManager"]);
        var staffId = await Seed("staff@example.test", ["LearningFacilitator"]);
        using var manager = Client(); await Login(manager, "manager@example.test");
        using var staff = Client(); await Login(staff, "staff@example.test");
        Assert.Equal(HttpStatusCode.NoContent, (await Post(manager, $"/api/administration/users/{staffId}/course-author-role", new { granted = true, reason = "Assign course work" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staff.GetAsync("/api/auth/session")).StatusCode);
        await Login(staff, "staff@example.test"); var course = await Create(staff, staffId);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(manager, "/api/authoring/courses", new CreateCourse(Guid.NewGuid(), "No", "", staffId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(manager, $"/api/authoring/courses/{course.Course.Id}/draft", ValidDraft())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(manager, $"/api/administration/users/{managerId}/course-author-role", new { granted = true, reason = "Inject", roles = new[] { "Administrator" } })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(manager, $"/api/administration/users/{managerId}/administrator-role", new { granted = true, currentPassword = Password, reason = "Escalate" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(manager, $"/api/administration/users/{staffId}/course-author-role", new { granted = false, reason = "End authoring assignment" })).StatusCode);
        await Login(staff, "staff@example.test");
        Assert.Equal(HttpStatusCode.NotFound, (await staff.GetAsync($"/api/authoring/courses/{course.Course.Id}")).StatusCode);
        Assert.Equal(1, await Count("SELECT count(*) FROM authoring.course_authors"));
    }

    [Fact]
    public async Task Publishing_rejects_empty_notes_and_zero_required_denominator()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); var course = await Create(admin, await UserId(admin));
        var path = $"/api/authoring/courses/{course.Course.Id}";
        await Post(admin, path + "/draft", ValidDraft(notes: "  "));
        var empty = await Post(admin, path + "/publish", new PublishCourse(2));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode); Assert.Contains("lesson_requires_content", await empty.Content.ReadAsStringAsync());
        await Post(admin, path + "/draft", ValidDraft(2, required: false));
        var optional = await Post(admin, path + "/publish", new PublishCourse(3));
        Assert.Equal(HttpStatusCode.BadRequest, optional.StatusCode); Assert.Contains("course_requires_required_lesson", await optional.Content.ReadAsStringAsync());
        Assert.Equal(0, await Count("SELECT count(*) FROM authoring.courses WHERE status='published'"));
    }

    [Fact]
    public async Task Create_and_save_retries_do_not_duplicate_and_competing_edits_conflict()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); var owner = await UserId(admin); var id = Guid.NewGuid();
        await Task.WhenAll(Create(admin, owner, id), Create(admin, owner, id));
        var path = $"/api/authoring/courses/{id}"; var draft = ValidDraft();
        Assert.Equal(HttpStatusCode.OK, (await Post(admin, path + "/draft", draft)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post(admin, path + "/draft", draft)).StatusCode);
        var responses = await Task.WhenAll(Post(admin, path + "/draft", draft with { ExpectedRevision = 2, Title = "Edit A" }), Post(admin, path + "/draft", draft with { ExpectedRevision = 2, Title = "Edit B" }));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, await Count("SELECT count(*) FROM authoring.courses"));
        Assert.Equal(2, await Count("SELECT count(*) FROM identity.audit_log WHERE action='course.draft_saved'"));
    }

    [Fact]
    public async Task Content_ids_cannot_be_borrowed_from_another_course_and_positions_reorder_atomically()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); var owner = await UserId(admin);
        var first = await Create(admin, owner); var second = await Create(admin, owner); var draft = ValidDraft();
        var secondModule = new ModuleDraft(Guid.NewGuid(), "Second", [new(Guid.NewGuid(), "Second lesson", "Content", true)]);
        draft = draft with { Modules = [draft.Modules[0], secondModule] };
        var firstPath = $"/api/authoring/courses/{first.Course.Id}/draft";
        Assert.Equal(HttpStatusCode.OK, (await Post(admin, firstPath, draft)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post(admin, firstPath, draft with { ExpectedRevision = 2, Modules = [secondModule, draft.Modules[0]] })).StatusCode);
        var conflict = await Post(admin, $"/api/authoring/courses/{second.Course.Id}/draft", draft);
        Assert.Equal(HttpStatusCode.BadRequest, conflict.StatusCode);
        Assert.Equal(2, await Count("SELECT count(*) FROM authoring.modules"));
        var read = (await admin.GetFromJsonAsync<CourseDetail>($"/api/authoring/courses/{first.Course.Id}"))!;
        Assert.Equal(secondModule.Id, read.Modules[0].Id);
    }

    [Fact]
    public async Task Audit_failure_rolls_back_course_structure_and_publication()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); var owner = await UserId(admin);
        var course = await Create(admin, owner); var path = $"/api/authoring/courses/{course.Course.Id}";
        await Post(admin, path + "/draft", ValidDraft());
        await Execute("REVOKE INSERT ON identity.audit_log FROM variable_runtime");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Post(admin, path + "/publish", new PublishCourse(2))).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Post(admin, path + "/draft", ValidDraft(2))).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Post(admin, "/api/authoring/courses", new CreateCourse(Guid.NewGuid(), "Rollback", "", owner))).StatusCode);
        Assert.Equal(1, await Count("SELECT count(*) FROM authoring.courses WHERE status='draft' AND revision=2"));
        Assert.Equal(1, await Count("SELECT count(*) FROM authoring.lessons"));
    }

    [Fact]
    public async Task Ownership_transfer_rechecks_eligibility_and_serializes_competing_requests()
    {
        await Bootstrap(); var first = await Seed("first@example.test", ["CourseAuthor"]); var second = await Seed("second@example.test", ["CourseAuthor"]);
        var managerId = await Seed("manager@example.test", ["OrganizationManager"]);
        using var admin = Client(); await Login(admin); var course = await Create(admin, first);
        using var manager = Client(); await Login(manager, "manager@example.test"); var path = $"/api/authoring/courses/{course.Course.Id}/owner";
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(manager, path, new ChangeOwner(1, managerId, "Missing author role"))).StatusCode);
        var responses = await Task.WhenAll(Post(manager, path, new ChangeOwner(1, second, "Transfer")), Post(admin, path, new ChangeOwner(1, await UserId(admin), "Transfer")));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, await Count("SELECT count(*) FROM authoring.course_authors"));
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.audit_log WHERE action='course.owner_changed'"));
    }

    [Fact]
    public async Task Schema_requires_one_owner_and_runtime_cannot_delete_course_history()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); await Create(admin, await UserId(admin));
        await using var connection = new NpgsqlConnection(RuntimeConnection); await connection.OpenAsync();
        await using var runtimeDelete = new NpgsqlCommand("DELETE FROM authoring.courses", connection);
        Assert.Equal("42501", (await Assert.ThrowsAsync<PostgresException>(() => runtimeDelete.ExecuteNonQueryAsync())).SqlState);
        var error = await Assert.ThrowsAsync<PostgresException>(() => Execute("DELETE FROM authoring.course_authors"));
        Assert.Equal("23503", error.SqlState);
    }

    [Fact]
    public async Task Foreign_course_fixtures_are_hidden_and_cross_organization_owner_links_are_rejected()
    {
        await Bootstrap(); var foreignOrganization = Guid.NewGuid();
        var foreignOwner = await Seed("foreign@example.test", ["CourseAuthor"], foreignOrganization); var foreignCourse = Guid.NewGuid();
        await Execute($"""
            BEGIN;
            INSERT INTO authoring.courses(organization_id,id,title,description,status,created_by,create_hash,created_at,updated_at)
            VALUES ('{foreignOrganization}','{foreignCourse}','Foreign course','','draft','{foreignOwner}','fixture',now(),now());
            INSERT INTO authoring.course_authors(id,organization_id,course_id,user_id,created_at)
            VALUES ('{Guid.NewGuid()}','{foreignOrganization}','{foreignCourse}','{foreignOwner}',now());
            COMMIT;
            """);
        using var admin = Client(); await Login(admin);
        Assert.Empty((await admin.GetFromJsonAsync<CourseSummary[]>("/api/authoring/courses"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/authoring/courses/{foreignCourse}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(admin, $"/api/authoring/courses/{foreignCourse}/draft", ValidDraft())).StatusCode);
        var local = await Create(admin, await UserId(admin));
        var error = await Assert.ThrowsAsync<PostgresException>(() => Execute($"UPDATE authoring.course_authors SET user_id='{foreignOwner}' WHERE course_id='{local.Course.Id}'"));
        Assert.Equal("23503", error.SqlState);
    }

    [Fact]
    public async Task Authoring_requires_csrf_and_rejects_parent_status_and_owner_payload_injection()
    {
        await Bootstrap(); using var admin = Client(); await Login(admin); var actor = await UserId(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/authoring/courses", new CreateCourse(Guid.NewGuid(), "No CSRF", "", actor))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(admin, "/api/authoring/courses", new { id = Guid.NewGuid(), title = "Injected", description = "", ownerId = actor, status = "published", organizationId = organization })).StatusCode);
        var course = await Create(admin, actor);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(admin, $"/api/authoring/courses/{course.Course.Id}/draft", new { expectedRevision = 1, title = "Injected", description = "", modules = Array.Empty<object>(), ownerId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(0, await Count("SELECT count(*) FROM authoring.courses WHERE status='published'"));
    }

    [Fact]
    public async Task Upgrade_from_foundation_preserves_identity_and_old_manifest_refuses_new_schema()
    {
        // Reconstruct the empty later module areas to exercise the actual v1 upgrade path.
        await Bootstrap();
        await Execute("DROP SCHEMA enrollment CASCADE; DROP SCHEMA authoring CASCADE; DELETE FROM platform.schema_migrations WHERE version>=2");
        var old = new MigrationPlan(IdentityModule.Migration);
        Assert.True(await old.ReadyAsync(RuntimeConnection));
        using var client = Client(); Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
        await ApplicationMigrations.Plan.ApplyAsync(MigrationConnection, "variable_runtime");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.users"));
        Assert.False(await old.ReadyAsync(RuntimeConnection));
        await Assert.ThrowsAsync<InvalidOperationException>(() => old.ApplyAsync(MigrationConnection, "variable_runtime"));
    }
}

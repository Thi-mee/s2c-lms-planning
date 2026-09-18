using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Variable.Identity;

namespace Variable.Authoring;

internal sealed class AuthoringFailure(string code, int status) : Exception(code)
{
    internal string Code { get; } = code;
    internal int Status { get; } = status;
}

internal sealed class AuthoringService(IIdentityAccess identity, IConfiguration configuration, TimeProvider clock)
{
    private const string SelectCourse = """
        SELECT c.id, c.title, c.description, c.status, a.user_id, c.revision, c.requirements_version, c.created_at
        FROM authoring.courses c JOIN authoring.course_authors a ON (a.organization_id, a.course_id) = (c.organization_id, c.id)
        """;

    internal async Task<CourseSummary[]> ListAsync(ClaimsPrincipal principal, int offset, CancellationToken ct)
    {
        if (offset < 0 || offset > 100000) throw new AuthoringFailure("invalid_offset", 400);
        var actor = await identity.CurrentAsync(principal, ct);
        RequireLibraryAccess(actor);
        await using var connection = await OpenAsync(ct);
        await using var command = Command(connection, null, SelectCourse + """
             WHERE c.organization_id = $1 AND ($2 OR ($3 AND a.user_id = $4) OR ($5 AND c.status = 'published'))
             ORDER BY c.created_at DESC, c.id LIMIT 50 OFFSET $6
            """, actor.Organization.Id, Has(actor, "Administrator") || Has(actor, "OrganizationManager"), Has(actor, "CourseAuthor"),
            actor.Account.Id, Has(actor, "CohortCoordinator"), offset);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<CourseSummary>();
        while (await reader.ReadAsync(ct)) rows.Add(ReadSummary(reader));
        return rows.ToArray();
    }

    internal async Task<CourseDetail> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken ct)
    {
        var actor = await identity.CurrentAsync(principal, ct);
        await using var connection = await OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        return await LoadAsync(connection, transaction, actor, id, ct);
    }

    internal async Task<CourseDetail> CreateAsync(ClaimsPrincipal principal, CreateCourse request, CancellationToken ct)
    {
        ValidateText(request.Title, request.Description);
        if (request.Id == Guid.Empty || request.OwnerId == Guid.Empty) throw new AuthoringFailure("invalid_identifier", 400);
        await using var work = await identity.BeginWriteAsync(principal, ct);
        if (!Has(work.Actor, "Administrator") && (!Has(work.Actor, "CourseAuthor") || request.OwnerId != work.Actor.Account.Id))
            throw new AuthoringFailure("forbidden", 403);
        await work.RequireEligibleOwnerAsync(request.OwnerId, ct);
        var hash = Hash(new { actor = work.Actor.Account.Id, request });
        await using (var existing = WriteCommand(work, "SELECT create_hash FROM authoring.courses WHERE organization_id = $1 AND id = $2", work.Actor.Organization.Id, request.Id))
        {
            if (await existing.ExecuteScalarAsync(ct) is string previous)
            {
                if (previous != hash) throw new AuthoringFailure("request_conflict", 409);
                return await LoadAsync(work, request.Id, ct);
            }
        }
        var now = clock.GetUtcNow();
        await ExecuteAsync(work, """
            INSERT INTO authoring.courses(organization_id,id,title,description,status,created_by,create_hash,created_at,updated_at)
            VALUES ($1,$2,$3,$4,'draft',$5,$6,$7,$7)
            """, ct, work.Actor.Organization.Id, request.Id, request.Title.Trim(), request.Description.Trim(), work.Actor.Account.Id, hash, now);
        await ExecuteAsync(work, """
            INSERT INTO authoring.course_authors(id,organization_id,course_id,user_id,created_at) VALUES ($1,$2,$3,$4,$5)
            """, ct, Guid.NewGuid(), work.Actor.Organization.Id, request.Id, request.OwnerId, now);
        await work.AuditAsync("course.created", request.Id, new { ownerId = request.OwnerId, revision = 1 }, ct);
        var result = await LoadAsync(work, request.Id, ct);
        await work.CommitAsync(ct);
        return result;
    }

    internal async Task<CourseDetail> SaveAsync(ClaimsPrincipal principal, Guid id, SaveCourse request, CancellationToken ct)
    {
        ValidateDraft(request);
        await using var work = await identity.BeginWriteAsync(principal, ct);
        var current = await LoadAsync(work, id, ct);
        RequireEditor(work.Actor, current.Course);
        if (current.Course.Status != "draft") throw new AuthoringFailure("published_content_read_only", 409);
        var previousParents = current.Modules.SelectMany(module => module.Lessons.Select(lesson => (lesson.Id, ModuleId: module.Id))).ToDictionary(x => x.Id, x => x.ModuleId);
        if (request.Modules.Any(module => module.Lessons.Any(lesson => previousParents.TryGetValue(lesson.Id, out var parent) && parent != module.Id)))
            throw new AuthoringFailure("lesson_parent_is_immutable", 400);
        var hash = Hash(new { actor = work.Actor.Account.Id, id, request });
        if (current.Course.Revision != request.ExpectedRevision)
        {
            await using var retry = WriteCommand(work, "SELECT last_save_hash = $3 AND last_save_revision = $4 FROM authoring.courses WHERE organization_id = $1 AND id = $2",
                work.Actor.Organization.Id, id, hash, request.ExpectedRevision);
            if (await retry.ExecuteScalarAsync(ct) is true) return current;
            throw new AuthoringFailure("revision_conflict", 409);
        }
        // Only unpublished draft content can be removed. No cascade can erase future learning records.
        var moduleIds = request.Modules.Select(x => x.Id).ToArray();
        var lessonIds = request.Modules.SelectMany(x => x.Lessons).Select(x => x.Id).ToArray();
        await ExecuteAsync(work, "DELETE FROM authoring.lessons WHERE organization_id = $1 AND course_id = $2 AND NOT(id = ANY($3))", ct, work.Actor.Organization.Id, id, lessonIds);
        await ExecuteAsync(work, "DELETE FROM authoring.modules WHERE organization_id = $1 AND course_id = $2 AND NOT(id = ANY($3))", ct, work.Actor.Organization.Id, id, moduleIds);
        var now = clock.GetUtcNow();
        for (var position = 0; position < request.Modules.Length; position++)
        {
            var module = request.Modules[position];
            var changed = await ExecuteAsync(work, """
                INSERT INTO authoring.modules(organization_id,course_id,id,title,position,created_at,updated_at) VALUES ($1,$2,$3,$4,$5,$6,$6)
                ON CONFLICT (organization_id,id) DO UPDATE SET title = EXCLUDED.title, position = EXCLUDED.position, updated_at = EXCLUDED.updated_at
                WHERE authoring.modules.course_id = EXCLUDED.course_id
                """, ct, work.Actor.Organization.Id, id, module.Id, module.Title.Trim(), position, now);
            if (changed != 1) throw new AuthoringFailure("draft_identifier_conflict", 400);
            for (var lessonPosition = 0; lessonPosition < module.Lessons.Length; lessonPosition++)
            {
                var lesson = module.Lessons[lessonPosition];
                changed = await ExecuteAsync(work, """
                    INSERT INTO authoring.lessons(organization_id,course_id,module_id,id,title,notes,required,position,created_at,updated_at)
                    VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$9)
                    ON CONFLICT (organization_id,id) DO UPDATE SET module_id = EXCLUDED.module_id, title = EXCLUDED.title,
                        notes = EXCLUDED.notes, required = EXCLUDED.required, position = EXCLUDED.position, updated_at = EXCLUDED.updated_at,
                        content_version = authoring.lessons.content_version + CASE WHEN (authoring.lessons.title,authoring.lessons.notes,authoring.lessons.required)
                            IS DISTINCT FROM (EXCLUDED.title,EXCLUDED.notes,EXCLUDED.required) THEN 1 ELSE 0 END
                    WHERE authoring.lessons.course_id = EXCLUDED.course_id
                    """, ct, work.Actor.Organization.Id, id, module.Id, lesson.Id, lesson.Title.Trim(), lesson.Notes, lesson.Required, lessonPosition, now);
                if (changed != 1) throw new AuthoringFailure("draft_identifier_conflict", 400);
            }
        }
        await ExecuteAsync(work, """
            UPDATE authoring.courses SET title=$3,description=$4,revision=revision+1,requirements_version=requirements_version+1,
                last_save_hash=$5,last_save_revision=$6,updated_at=$7 WHERE organization_id=$1 AND id=$2
            """, ct, work.Actor.Organization.Id, id, request.Title.Trim(), request.Description.Trim(), hash, request.ExpectedRevision, now);
        await work.AuditAsync("course.draft_saved", id, new { previousRevision = current.Course.Revision, moduleCount = moduleIds.Length, lessonCount = lessonIds.Length }, ct);
        var result = await LoadAsync(work, id, ct);
        await work.CommitAsync(ct);
        return result;
    }

    internal async Task<CourseDetail> PublishAsync(ClaimsPrincipal principal, Guid id, long expectedRevision, CancellationToken ct)
    {
        await using var work = await identity.BeginWriteAsync(principal, ct);
        var current = await LoadAsync(work, id, ct);
        RequireEditor(work.Actor, current.Course);
        if (current.Course.Status == "published" && current.Course.Revision == expectedRevision + 1) return current;
        RequireRevision(current.Course, expectedRevision);
        if (current.Course.Status != "draft") throw new AuthoringFailure("published_content_read_only", 409);
        await work.RequireEligibleOwnerAsync(current.Course.OwnerId, ct);
        if (current.Modules.Length == 0 || current.Modules.Any(x => x.Lessons.Length == 0)) throw new AuthoringFailure("course_requires_lessons", 400);
        var lessons = current.Modules.SelectMany(x => x.Lessons).ToArray();
        if (lessons.Any(x => string.IsNullOrWhiteSpace(x.Notes))) throw new AuthoringFailure("lesson_requires_content", 400);
        if (!lessons.Any(x => x.Required)) throw new AuthoringFailure("course_requires_required_lesson", 400);
        await ExecuteAsync(work, "UPDATE authoring.courses SET status='published',revision=revision+1,published_at=$3,updated_at=$3 WHERE organization_id=$1 AND id=$2",
            ct, work.Actor.Organization.Id, id, clock.GetUtcNow());
        await work.AuditAsync("course.published", id, new { requirementsVersion = current.Course.RequirementsVersion, requiredLessons = lessons.Count(x => x.Required) }, ct);
        var result = await LoadAsync(work, id, ct);
        await work.CommitAsync(ct);
        return result;
    }

    internal async Task<CourseDetail> ChangeOwnerAsync(ClaimsPrincipal principal, Guid id, ChangeOwner request, CancellationToken ct)
    {
        if (request.OwnerId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            throw new AuthoringFailure("invalid_owner_change", 400);
        await using var work = await identity.BeginWriteAsync(principal, ct);
        var current = await LoadAsync(work, id, ct);
        if (!Has(work.Actor, "OrganizationManager")) RequireEditor(work.Actor, current.Course);
        RequireRevision(current.Course, request.ExpectedRevision);
        await work.RequireEligibleOwnerAsync(request.OwnerId, ct);
        if (current.Course.OwnerId == request.OwnerId) return current;
        await ExecuteAsync(work, "UPDATE authoring.course_authors SET user_id=$3 WHERE organization_id=$1 AND course_id=$2", ct, work.Actor.Organization.Id, id, request.OwnerId);
        await ExecuteAsync(work, "UPDATE authoring.courses SET revision=revision+1,updated_at=$3 WHERE organization_id=$1 AND id=$2", ct, work.Actor.Organization.Id, id, clock.GetUtcNow());
        await work.AuditAsync("course.owner_changed", id, new { before = current.Course.OwnerId, after = request.OwnerId, request.Reason }, ct);
        // The former owner may no longer read the course after commit; return the authorized transition's result.
        var result = current with { Course = current.Course with { OwnerId = request.OwnerId, Revision = current.Course.Revision + 1 } };
        await work.CommitAsync(ct);
        return result;
    }

    private Task<CourseDetail> LoadAsync(IdentityWork work, Guid id, CancellationToken ct) =>
        LoadAsync((NpgsqlConnection)work.Connection, (NpgsqlTransaction)work.Transaction, work.Actor, id, ct);

    private static async Task<CourseDetail> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, SessionView actor, Guid id, CancellationToken ct)
    {
        CourseSummary course;
        await using (var command = Command(connection, transaction, SelectCourse + " WHERE c.organization_id=$1 AND c.id=$2", actor.Organization.Id, id))
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) throw new AuthoringFailure("course_not_found", 404);
            course = ReadSummary(reader);
        }
        if (!CanRead(actor, course)) throw new AuthoringFailure("course_not_found", 404);
        var modules = new List<(Guid Id, string Title)>();
        await using (var command = Command(connection, transaction, "SELECT id,title FROM authoring.modules WHERE organization_id=$1 AND course_id=$2 ORDER BY position", actor.Organization.Id, id))
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) modules.Add((reader.GetGuid(0), reader.GetString(1)));
        var lessons = new List<(Guid ModuleId, LessonDraft Lesson)>();
        await using (var command = Command(connection, transaction, "SELECT module_id,id,title,notes,required FROM authoring.lessons WHERE organization_id=$1 AND course_id=$2 ORDER BY position", actor.Organization.Id, id))
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) lessons.Add((reader.GetGuid(0), new(reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetBoolean(4))));
        return new(course, modules.Select(module => new ModuleDraft(module.Id, module.Title, lessons.Where(x => x.ModuleId == module.Id).Select(x => x.Lesson).ToArray())).ToArray());
    }

    private static bool Has(SessionView actor, string role) => actor.Account.Roles.Contains(role);
    private static bool CanRead(SessionView actor, CourseSummary course) => Has(actor, "Administrator") || Has(actor, "OrganizationManager")
        || (Has(actor, "CourseAuthor") && course.OwnerId == actor.Account.Id) || (Has(actor, "CohortCoordinator") && course.Status == "published");
    private static void RequireLibraryAccess(SessionView actor)
    {
        if (!actor.Account.Roles.Intersect(["Administrator", "OrganizationManager", "CourseAuthor", "CohortCoordinator"]).Any()) throw new AuthoringFailure("forbidden", 403);
    }
    private static void RequireEditor(SessionView actor, CourseSummary course)
    {
        if (!Has(actor, "Administrator") && !(Has(actor, "CourseAuthor") && course.OwnerId == actor.Account.Id)) throw new AuthoringFailure("forbidden", 403);
    }
    private static void RequireRevision(CourseSummary course, long expected)
    { if (expected != course.Revision) throw new AuthoringFailure("revision_conflict", 409); }
    private static void ValidateText(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200 || description is null || description.Length > 4000)
            throw new AuthoringFailure("invalid_course_details", 400);
    }
    private static void ValidateDraft(SaveCourse request)
    {
        ValidateText(request.Title, request.Description);
        if (request.ExpectedRevision < 1 || request.Modules is null || request.Modules.Length > 50
            || request.Modules.Any(x => x is null || x.Id == Guid.Empty || string.IsNullOrWhiteSpace(x.Title) || x.Title.Length > 200 || x.Lessons is null)
            || request.Modules.Select(x => x.Id).Distinct().Count() != request.Modules.Length)
            throw new AuthoringFailure("invalid_modules", 400);
        var lessons = request.Modules.SelectMany(x => x.Lessons).ToArray();
        if (lessons.Length > 200 || lessons.Any(x => x is null || x.Id == Guid.Empty || string.IsNullOrWhiteSpace(x.Title) || x.Title.Length > 200
            || x.Notes is null || x.Notes.Length > 50000) || lessons.Select(x => x.Id).Distinct().Count() != lessons.Length
            || lessons.Sum(x => (long)x.Notes.Length) > 500000)
            throw new AuthoringFailure("invalid_lessons", 400);
    }
    private static CourseSummary ReadSummary(NpgsqlDataReader reader) => new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
        reader.GetGuid(4), reader.GetInt64(5), reader.GetInt64(6), reader.GetFieldValue<DateTimeOffset>(7));
    private static string Hash(object payload) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload)));
    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new NpgsqlConnection(configuration.GetConnectionString("Runtime"));
        try { await connection.OpenAsync(ct); return connection; } catch { await connection.DisposeAsync(); throw; }
    }
    private static NpgsqlCommand Command(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql, params object[] values)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var value in values) command.Parameters.AddWithValue(value);
        return command;
    }
    private static NpgsqlCommand WriteCommand(IdentityWork work, string sql, params object[] values) => Command((NpgsqlConnection)work.Connection, (NpgsqlTransaction)work.Transaction, sql, values);
    private static async Task<int> ExecuteAsync(IdentityWork work, string sql, CancellationToken ct, params object[] values)
    { await using var command = WriteCommand(work, sql, values); return await command.ExecuteNonQueryAsync(ct); }
}

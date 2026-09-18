using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Variable.Authoring;
using Variable.Identity;

namespace Variable.Enrollment;

internal sealed class EnrollmentFailure(string code, int status) : Exception(code)
{
    internal string Code { get; } = code;
    internal int Status { get; } = status;
}

internal sealed class EnrollmentService(IIdentityAccess identity, IAuthoringAccess authoring, IConfiguration configuration, TimeProvider clock)
{
    private const string SelectCohort = """
        SELECT c.id,c.course_id,p.title,c.title,c.start_at,c.end_at,c.revision,c.created_at
        FROM enrollment.cohorts c
        JOIN authoring.courses p ON (p.organization_id,p.id)=(c.organization_id,c.course_id)
        """;

    internal async Task<CohortSummary[]> ListAsync(ClaimsPrincipal principal, int offset, CancellationToken ct)
    {
        if (offset < 0 || offset > 100000) throw new EnrollmentFailure("invalid_offset", 400);
        var actor = await identity.CurrentAsync(principal, ct);
        RequireViewer(actor);
        await using var connection = await OpenAsync(ct);
        await using var command = Command(connection, null, SelectCohort + """
            WHERE c.organization_id=$1 AND ($2 OR EXISTS (
                SELECT 1 FROM enrollment.cohort_staff s
                WHERE s.organization_id=c.organization_id AND s.cohort_id=c.id AND s.user_id=$3
                  AND ((s.capability='coordinator' AND $4) OR (s.capability='facilitator' AND $5))))
            ORDER BY c.created_at DESC,c.id LIMIT 50 OFFSET $6
            """, actor.Organization.Id, IsOrganizationOperator(actor), actor.Account.Id,
            Has(actor, AccountRole.CohortCoordinator), Has(actor, AccountRole.LearningFacilitator), offset);
        var rows = new List<CohortRow>();
        await using (var reader = await command.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) rows.Add(ReadRow(reader));
        var staff = await LoadStaffAsync(connection, null, actor.Organization.Id, rows.Select(x => x.Id).ToArray(), ct);
        return rows.Select(row => View(row, staff.GetValueOrDefault(row.Id, []))).ToArray();
    }

    internal async Task<CohortSummary> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken ct)
    {
        var actor = await identity.CurrentAsync(principal, ct);
        await using var connection = await OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        return await LoadAsync(connection, transaction, actor, id, true, ct);
    }

    internal async Task<CohortSummary> CreateAsync(ClaimsPrincipal principal, CreateCohort request, CancellationToken ct)
    {
        var normalized = Validate(request);
        await using var work = await identity.BeginWriteAsync(principal, ct);
        RequireCreator(work.Actor);
        var course = await authoring.FindPublishedCourseAsync(work, normalized.CourseId, ct)
            ?? throw new EnrollmentFailure("published_course_not_found", 404);
        var coordinator = await work.RequireEligibleCohortStaffAsync(normalized.CoordinatorId, "coordinator", ct);
        AccountView? facilitator = null;
        if (normalized.FacilitatorId is { } facilitatorId)
            facilitator = await work.RequireEligibleCohortStaffAsync(facilitatorId, "facilitator", ct);
        var hash = Hash(new { actor = work.Actor.Account.Id, request = normalized });
        await using (var existing = WriteCommand(work, "SELECT create_hash FROM enrollment.cohorts WHERE organization_id=$1 AND id=$2",
            work.Actor.Organization.Id, normalized.Id))
        {
            if (await existing.ExecuteScalarAsync(ct) is string previous)
            {
                if (previous != hash) throw new EnrollmentFailure("request_conflict", 409);
                return await LoadAsync(work, normalized.Id, false, ct);
            }
        }
        var now = clock.GetUtcNow();
        await ExecuteAsync(work, """
            INSERT INTO enrollment.cohorts(organization_id,id,course_id,title,start_at,end_at,created_by,create_hash,created_at,updated_at)
            VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$9)
            """, ct, work.Actor.Organization.Id, normalized.Id, normalized.CourseId, normalized.Title.Trim(), normalized.StartAt,
            normalized.EndAt, work.Actor.Account.Id, hash, now);
        await InsertStaffAsync(work, normalized.Id, coordinator.Id, "coordinator", now, ct);
        if (facilitator is not null) await InsertStaffAsync(work, normalized.Id, facilitator.Id, "facilitator", now, ct);
        await work.AuditAsync("cohort.created", normalized.Id, new
        {
            courseId = course.Id, coordinatorId = coordinator.Id, facilitatorId = facilitator?.Id,
            startAt = normalized.StartAt, endAt = normalized.EndAt
        }, ct);
        var result = await LoadAsync(work, normalized.Id, false, ct);
        await work.CommitAsync(ct);
        return result;
    }

    internal async Task<CohortSummary> ChangeStaffAsync(ClaimsPrincipal principal, Guid id, ChangeCohortStaff request, CancellationToken ct)
    {
        if (request.ExpectedRevision < 1 || request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            throw new EnrollmentFailure("invalid_staff_change", 400);
        var capability = NormalizeCapability(request.Capability);
        await using var work = await identity.BeginWriteAsync(principal, ct);
        var current = await LoadAsync(work, id, true, ct);
        RequireStaffManager(work.Actor, current);
        var assigned = current.Staff.Any(x => x.UserId == request.UserId && x.Capability == capability);
        if (current.Revision != request.ExpectedRevision)
        {
            if (assigned == request.Assigned) return current;
            throw new EnrollmentFailure("revision_conflict", 409);
        }
        if (assigned == request.Assigned) return current;
        CohortStaffView? changedStaff = null;
        if (request.Assigned)
        {
            var target = await work.RequireEligibleCohortStaffAsync(request.UserId, capability, ct);
            await InsertStaffAsync(work, id, target.Id, capability, clock.GetUtcNow(), ct);
            changedStaff = new(target.Id, target.Name, target.Email, capability, true);
        }
        else
        {
            if (capability == "coordinator" && current.EndAt > clock.GetUtcNow()
                && !current.Staff.Any(x => x.Capability == "coordinator" && x.UserId != request.UserId && x.Effective))
                throw new EnrollmentFailure("cohort_requires_coordinator", 409);
            await ExecuteAsync(work, "DELETE FROM enrollment.cohort_staff WHERE organization_id=$1 AND cohort_id=$2 AND user_id=$3 AND capability=$4",
                ct, work.Actor.Organization.Id, id, request.UserId, capability);
        }
        await ExecuteAsync(work, "UPDATE enrollment.cohorts SET revision=revision+1,updated_at=$3 WHERE organization_id=$1 AND id=$2",
            ct, work.Actor.Organization.Id, id, clock.GetUtcNow());
        await work.AuditAsync(request.Assigned ? "cohort.staff_assigned" : "cohort.staff_revoked", id,
            new { userId = request.UserId, capability, request.Reason }, ct);
        var staff = request.Assigned ? [.. current.Staff, changedStaff!] : current.Staff.Where(x => x.UserId != request.UserId || x.Capability != capability).ToArray();
        var result = current with { Revision = current.Revision + 1, Staff = staff };
        await work.CommitAsync(ct);
        return result;
    }

    private Task<CohortSummary> LoadAsync(IdentityWork work, Guid id, bool authorize, CancellationToken ct) =>
        LoadAsync((NpgsqlConnection)work.Connection, (NpgsqlTransaction)work.Transaction, work.Actor, id, authorize, ct);

    private async Task<CohortSummary> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, SessionView actor,
        Guid id, bool authorize, CancellationToken ct)
    {
        CohortRow row;
        await using (var command = Command(connection, transaction, SelectCohort + " WHERE c.organization_id=$1 AND c.id=$2", actor.Organization.Id, id))
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) throw new EnrollmentFailure("cohort_not_found", 404);
            row = ReadRow(reader);
        }
        var staff = await LoadStaffAsync(connection, transaction, actor.Organization.Id, [id], ct);
        var result = View(row, staff.GetValueOrDefault(id, []));
        if (authorize && !CanRead(actor, result)) throw new EnrollmentFailure("cohort_not_found", 404);
        return result;
    }

    private static async Task<Dictionary<Guid, CohortStaffView[]>> LoadStaffAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        Guid organizationId, Guid[] cohortIds, CancellationToken ct)
    {
        if (cohortIds.Length == 0) return [];
        await using var command = Command(connection, transaction, """
            SELECT s.cohort_id,s.user_id,u.name,u.email,s.capability,
                   u.status='active' AND u.deleted_at IS NULL AND
                   CASE s.capability WHEN 'coordinator' THEN 'CohortCoordinator'=ANY(u.roles)
                                       ELSE 'LearningFacilitator'=ANY(u.roles) END
            FROM enrollment.cohort_staff s
            JOIN identity.users u ON (u.organization_id,u.id)=(s.organization_id,s.user_id)
            WHERE s.organization_id=$1 AND s.cohort_id=ANY($2)
            ORDER BY s.cohort_id,s.capability,u.normalized_email,u.id
            """, organizationId, cohortIds);
        var rows = new Dictionary<Guid, List<CohortStaffView>>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var cohortId = reader.GetGuid(0);
            if (!rows.TryGetValue(cohortId, out var list)) rows[cohortId] = list = [];
            list.Add(new(reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetBoolean(5)));
        }
        return rows.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }

    private static CreateCohort Validate(CreateCohort request)
    {
        if (request.Id == Guid.Empty || request.CourseId == Guid.Empty || request.CoordinatorId == Guid.Empty
            || request.FacilitatorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
            throw new EnrollmentFailure("invalid_cohort", 400);
        var start = request.StartAt.ToUniversalTime();
        var end = request.EndAt.ToUniversalTime();
        if (end < start) throw new EnrollmentFailure("invalid_schedule", 400);
        return request with { Title = request.Title.Trim(), StartAt = start, EndAt = end };
    }

    private static string NormalizeCapability(string capability) => capability?.Trim().ToLowerInvariant() switch
    {
        "coordinator" => "coordinator",
        "facilitator" => "facilitator",
        _ => throw new EnrollmentFailure("invalid_staff_capability", 400)
    };

    private static void RequireViewer(SessionView actor)
    {
        if (!actor.Account.Roles.Intersect([nameof(AccountRole.Administrator), nameof(AccountRole.OrganizationManager),
            nameof(AccountRole.CohortCoordinator), nameof(AccountRole.LearningFacilitator)]).Any())
            throw new EnrollmentFailure("forbidden", 403);
    }

    private static void RequireCreator(SessionView actor)
    {
        if (IsOrganizationOperator(actor)) return;
        if (!Has(actor, AccountRole.CohortCoordinator)) throw new EnrollmentFailure("forbidden", 403);
    }

    private static void RequireStaffManager(SessionView actor, CohortSummary cohort)
    {
        if (IsOrganizationOperator(actor)) return;
        if (!Has(actor, AccountRole.CohortCoordinator) || !cohort.Staff.Any(x => x.UserId == actor.Account.Id && x.Capability == "coordinator" && x.Effective))
            throw new EnrollmentFailure("forbidden", 403);
    }

    private static bool CanRead(SessionView actor, CohortSummary cohort) => IsOrganizationOperator(actor)
        || cohort.Staff.Any(x => x.UserId == actor.Account.Id && x.Effective
            && ((x.Capability == "coordinator" && Has(actor, AccountRole.CohortCoordinator))
                || (x.Capability == "facilitator" && Has(actor, AccountRole.LearningFacilitator))));

    private static bool IsOrganizationOperator(SessionView actor) => Has(actor, AccountRole.Administrator) || Has(actor, AccountRole.OrganizationManager);
    private static bool Has(SessionView actor, AccountRole role) => actor.Account.Roles.Contains(role.ToString());
    private CohortSummary View(CohortRow row, CohortStaffView[] staff) => new(row.Id, row.CourseId, row.CourseTitle, row.Title,
        row.StartAt, row.EndAt, row.StartAt > clock.GetUtcNow() ? "upcoming" : row.EndAt <= clock.GetUtcNow() ? "ended" : "active",
        row.Revision, row.CreatedAt, staff);
    private static CohortRow ReadRow(NpgsqlDataReader reader) => new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
        reader.GetFieldValue<DateTimeOffset>(4), reader.GetFieldValue<DateTimeOffset>(5), reader.GetInt64(6), reader.GetFieldValue<DateTimeOffset>(7));
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
    private static NpgsqlCommand WriteCommand(IdentityWork work, string sql, params object[] values) =>
        Command((NpgsqlConnection)work.Connection, (NpgsqlTransaction)work.Transaction, sql, values);
    private static async Task<int> ExecuteAsync(IdentityWork work, string sql, CancellationToken ct, params object[] values)
    { await using var command = WriteCommand(work, sql, values); return await command.ExecuteNonQueryAsync(ct); }
    private static Task InsertStaffAsync(IdentityWork work, Guid cohortId, Guid userId, string capability, DateTimeOffset now, CancellationToken ct) =>
        ExecuteAsync(work, "INSERT INTO enrollment.cohort_staff(id,organization_id,cohort_id,user_id,capability,created_at) VALUES ($1,$2,$3,$4,$5,$6)",
            ct, Guid.NewGuid(), work.Actor.Organization.Id, cohortId, userId, capability, now);

    private sealed record CohortRow(Guid Id, Guid CourseId, string CourseTitle, string Title, DateTimeOffset StartAt,
        DateTimeOffset EndAt, long Revision, DateTimeOffset CreatedAt);
}

internal sealed class CohortAccessRemovalGuard(TimeProvider clock) : IAccountAccessRemovalGuard
{
    public Task<string?> ValidateRoleRemovalAsync(IdentityWriteContext context, Guid userId, AccountRole role, CancellationToken ct) =>
        role == AccountRole.CohortCoordinator ? ValidateAsync(context, userId, ct) : Task.FromResult<string?>(null);

    public Task<string?> ValidateDeactivationAsync(IdentityWriteContext context, Guid userId, CancellationToken ct) => ValidateAsync(context, userId, ct);

    private async Task<string?> ValidateAsync(IdentityWriteContext context, Guid userId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM enrollment.cohorts c
                JOIN enrollment.cohort_staff mine ON (mine.organization_id,mine.cohort_id)=(c.organization_id,c.id)
                    AND mine.user_id=$2 AND mine.capability='coordinator'
                WHERE c.organization_id=$1 AND c.end_at>$3 AND NOT EXISTS (
                    SELECT 1 FROM enrollment.cohort_staff other
                    JOIN identity.users u ON (u.organization_id,u.id)=(other.organization_id,other.user_id)
                    WHERE other.organization_id=c.organization_id AND other.cohort_id=c.id
                      AND other.capability='coordinator' AND other.user_id<>$2
                      AND u.status='active' AND u.deleted_at IS NULL AND 'CohortCoordinator'=ANY(u.roles)))
            """, (NpgsqlConnection)context.Connection, (NpgsqlTransaction)context.Transaction);
        command.Parameters.AddWithValue(context.Actor.Organization.Id);
        command.Parameters.AddWithValue(userId);
        command.Parameters.AddWithValue(clock.GetUtcNow());
        return await command.ExecuteScalarAsync(ct) is true ? "cohort_requires_coordinator" : null;
    }
}

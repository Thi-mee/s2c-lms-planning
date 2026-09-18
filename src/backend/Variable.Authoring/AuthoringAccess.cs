using Npgsql;
using Variable.Identity;

namespace Variable.Authoring;

internal sealed class AuthoringAccess : IAuthoringAccess
{
    public async Task<PublishedCourseView?> FindPublishedCourseAsync(IdentityWork work, Guid courseId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT id, title FROM authoring.courses
            WHERE organization_id = $1 AND id = $2 AND status = 'published'
            """, (NpgsqlConnection)work.Connection, (NpgsqlTransaction)work.Transaction);
        command.Parameters.AddWithValue(work.Actor.Organization.Id);
        command.Parameters.AddWithValue(courseId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(reader.GetGuid(0), reader.GetString(1)) : null;
    }
}

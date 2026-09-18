using Npgsql;
using Variable.Database;

namespace Variable.Identity.Persistence;

internal static class DatabaseLifecycle
{
    internal static async Task<bool> ReadyAsync(InstallationOptions options, MigrationPlan plan, CancellationToken cancellationToken = default)
    {
        if (!await plan.ReadyAsync(options.ConnectionString, cancellationToken)) return false;
        await using var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var installation = new NpgsqlCommand("SELECT NOT EXISTS (SELECT FROM identity.installation WHERE organization_id <> $1)", connection);
        installation.Parameters.AddWithValue(options.OrganizationId);
        return await installation.ExecuteScalarAsync(cancellationToken) is true;
    }
}

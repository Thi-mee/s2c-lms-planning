using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Variable.Identity.Persistence;

internal static class DatabaseLifecycle
{
    internal const int CurrentVersion = 1;
    private const long MigrationLock = 817_601_001;

    internal static async Task MigrateAsync(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Migration")
            ?? throw new InvalidOperationException("ConnectionStrings:Migration is required for the explicit migrate command.");
        var runtimeRole = configuration["Database:RuntimeRole"]
            ?? throw new InvalidOperationException("Database:RuntimeRole is required for scoped grants.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var migrationLock = new NpgsqlCommand("SELECT pg_advisory_xact_lock($1)", connection, transaction);
        migrationLock.Parameters.AddWithValue(MigrationLock);
        await migrationLock.ExecuteNonQueryAsync();
        await using (var ledger = new NpgsqlCommand("""
            CREATE SCHEMA IF NOT EXISTS platform;
            CREATE TABLE IF NOT EXISTS platform.schema_migrations (
                version integer PRIMARY KEY, name text NOT NULL, sha256 varchar(64) NOT NULL,
                applied_at timestamptz NOT NULL DEFAULT now());
            """, connection, transaction)) await ledger.ExecuteNonQueryAsync();
        var assembly = typeof(DatabaseLifecycle).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(x => x.EndsWith("0001-identity.sql", StringComparison.Ordinal));
        await using var stream = assembly.GetManifestResourceStream(resource)!;
        var sql = await new StreamReader(stream).ReadToEndAsync();
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        await using (var existing = new NpgsqlCommand("SELECT version, sha256 FROM platform.schema_migrations ORDER BY version", connection, transaction))
        await using (var reader = await existing.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                if (reader.GetInt32(0) != CurrentVersion || reader.GetString(1) != checksum)
                    throw new InvalidOperationException("Unsupported schema or a shipped migration was modified. Restore/upgrade using the documented release path.");
        }
        await using var count = new NpgsqlCommand("SELECT count(*) FROM platform.schema_migrations", connection, transaction);
        if ((long)(await count.ExecuteScalarAsync())! == 0)
        {
            await using var migration = new NpgsqlCommand(sql, connection, transaction);
            await migration.ExecuteNonQueryAsync();
            await using var record = new NpgsqlCommand("INSERT INTO platform.schema_migrations(version, name, sha256) VALUES ($1, $2, $3)", connection, transaction);
            record.Parameters.AddWithValue(CurrentVersion);
            record.Parameters.AddWithValue("0001-identity");
            record.Parameters.AddWithValue(checksum);
            await record.ExecuteNonQueryAsync();
        }
        var role = new NpgsqlCommandBuilder().QuoteIdentifier(runtimeRole);
        await using var grants = new NpgsqlCommand($"""
            GRANT USAGE ON SCHEMA identity, platform TO {role};
            GRANT SELECT ON platform.schema_migrations TO {role};
            REVOKE ALL ON ALL TABLES IN SCHEMA identity FROM {role};
            GRANT SELECT, INSERT ON identity.organizations, identity.installation, identity.audit_log TO {role};
            -- SELECT FOR UPDATE uses this row as the organization security-write guard.
            GRANT UPDATE ON identity.organizations TO {role};
            GRANT SELECT, INSERT, UPDATE ON identity.users TO {role};
            GRANT SELECT, INSERT, UPDATE, DELETE ON identity.sessions TO {role};
            """, connection, transaction);
        await grants.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    internal static async Task<bool> ReadyAsync(InstallationOptions options, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var privilegeCheck = new NpgsqlCommand("""
            SELECT NOT (rolsuper OR rolbypassrls OR rolcreaterole OR rolcreatedb)
              AND NOT has_database_privilege(current_user, current_database(), 'CREATE')
              AND NOT EXISTS (SELECT FROM pg_namespace WHERE nspname IN ('identity','platform','public')
                    AND has_schema_privilege(current_user, oid, 'CREATE'))
              AND NOT EXISTS (SELECT FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname IN ('identity','platform') AND pg_has_role(current_user, c.relowner, 'MEMBER'))
            FROM pg_roles WHERE rolname = current_user
            """, connection);
        if (await privilegeCheck.ExecuteScalarAsync(cancellationToken) is not true) return false;
        await using var schema = new NpgsqlCommand("SELECT to_regclass('platform.schema_migrations') IS NOT NULL", connection);
        if (await schema.ExecuteScalarAsync(cancellationToken) is not true) return false;
        await using var version = new NpgsqlCommand("SELECT count(*) = 1 AND max(version) = $1 FROM platform.schema_migrations", connection);
        version.Parameters.AddWithValue(CurrentVersion);
        if (await version.ExecuteScalarAsync(cancellationToken) is not true) return false;
        await using var installation = new NpgsqlCommand("SELECT NOT EXISTS (SELECT FROM identity.installation WHERE organization_id <> $1)", connection);
        installation.Parameters.AddWithValue(options.OrganizationId);
        return await installation.ExecuteScalarAsync(cancellationToken) is true;
    }
}

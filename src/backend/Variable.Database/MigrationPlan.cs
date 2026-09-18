using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace Variable.Database;

public sealed record ModuleMigration(int Version, string Name, string Schema, string Sql, string Grants)
{
    public string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Sql)));

    public static ModuleMigration Embedded(Assembly assembly, int version, string name, string schema, string grants)
    {
        var resource = assembly.GetManifestResourceNames().Single(x => x.EndsWith(name + ".sql", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        return new(version, name, schema, new StreamReader(stream).ReadToEnd(), grants);
    }
}

// The host composes actual module migrations. No module knows another module's schema definition.
public sealed class MigrationPlan(params ModuleMigration[] migrations)
{
    private readonly ModuleMigration[] migrations = Validate(migrations);

    private static ModuleMigration[] Validate(ModuleMigration[] migrations)
    {
        var ordered = migrations.OrderBy(x => x.Version).ToArray();
        if (ordered.Length == 0 || !ordered.Select(x => x.Version).SequenceEqual(Enumerable.Range(1, ordered.Length)))
            throw new ArgumentException("Migrations must be a nonempty, contiguous version sequence.");
        return ordered;
    }

    public async Task ApplyAsync(string connectionString, string runtimeRole, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(runtimeRole)) throw new ArgumentException("Runtime role is required.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var guard = new NpgsqlCommand("SELECT pg_advisory_xact_lock(817601001)", connection, transaction))
            await guard.ExecuteNonQueryAsync(ct);
        await using (var ledger = new NpgsqlCommand("""
            CREATE SCHEMA IF NOT EXISTS platform;
            CREATE TABLE IF NOT EXISTS platform.schema_migrations (
                version integer PRIMARY KEY, name text NOT NULL, sha256 varchar(64) NOT NULL,
                applied_at timestamptz NOT NULL DEFAULT now());
            """, connection, transaction)) await ledger.ExecuteNonQueryAsync(ct);
        var applied = await AppliedAsync(connection, transaction, ct);
        if (!IsValidPrefix(applied)) throw new InvalidOperationException("Unsupported schema or modified migration history. Use a supported upgrade/restore path.");
        foreach (var migration in migrations.Skip(applied.Count))
        {
            await using (var command = new NpgsqlCommand(migration.Sql, connection, transaction)) await command.ExecuteNonQueryAsync(ct);
            await using var record = new NpgsqlCommand("INSERT INTO platform.schema_migrations(version, name, sha256) VALUES ($1, $2, $3)", connection, transaction);
            record.Parameters.AddWithValue(migration.Version); record.Parameters.AddWithValue(migration.Name); record.Parameters.AddWithValue(migration.Checksum);
            await record.ExecuteNonQueryAsync(ct);
        }
        var role = new NpgsqlCommandBuilder().QuoteIdentifier(runtimeRole);
        await using (var ledgerGrant = new NpgsqlCommand($"GRANT USAGE ON SCHEMA platform TO {role}; GRANT SELECT ON platform.schema_migrations TO {role}", connection, transaction))
            await ledgerGrant.ExecuteNonQueryAsync(ct);
        foreach (var migration in migrations)
        {
            await using var grants = new NpgsqlCommand(migration.Grants.Replace("{runtime_role}", role, StringComparison.Ordinal), connection, transaction);
            await grants.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    public async Task<bool> ReadyAsync(string connectionString, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(ct);
        await using var privileges = new NpgsqlCommand("""
            SELECT NOT (rolsuper OR rolbypassrls OR rolcreaterole OR rolcreatedb)
              AND NOT has_database_privilege(current_user, current_database(), 'CREATE')
              AND NOT EXISTS (SELECT FROM pg_namespace WHERE nspname = ANY($1)
                  AND has_schema_privilege(current_user, oid, 'CREATE'))
              AND NOT EXISTS (SELECT FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                  WHERE n.nspname = ANY($1) AND pg_has_role(current_user, c.relowner, 'MEMBER'))
            FROM pg_roles WHERE rolname = current_user
            """, connection);
        privileges.Parameters.AddWithValue(migrations.Select(x => x.Schema).Concat(["public", "platform"]).Distinct().ToArray());
        if (await privileges.ExecuteScalarAsync(ct) is not true) return false;
        await using var exists = new NpgsqlCommand("SELECT to_regclass('platform.schema_migrations') IS NOT NULL", connection);
        if (await exists.ExecuteScalarAsync(ct) is not true) return false;
        var applied = await AppliedAsync(connection, null, ct);
        return applied.Count == migrations.Length && IsValidPrefix(applied);
    }

    private bool IsValidPrefix(List<(int Version, string Name, string Checksum)> applied) => applied.Count <= migrations.Length
        && applied.Select((row, index) => row.Version == migrations[index].Version && row.Name == migrations[index].Name
            && row.Checksum == migrations[index].Checksum).All(valid => valid);

    private static async Task<List<(int Version, string Name, string Checksum)>> AppliedAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT version, name, sha256 FROM platform.schema_migrations ORDER BY version", connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<(int, string, string)>();
        while (await reader.ReadAsync(ct)) rows.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        return rows;
    }
}

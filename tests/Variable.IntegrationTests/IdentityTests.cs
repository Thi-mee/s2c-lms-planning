using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Variable.Identity;
using Xunit;

namespace Variable.IntegrationTests;

public sealed class IdentityTests : IAsyncLifetime
{
    private const string Password = "Synthetic-test-password-726!";
    private const string Email = "administrator@example.test";
    private readonly Guid organization = Guid.NewGuid();
    private readonly string database = "variable_test_" + Guid.NewGuid().ToString("N");
    private readonly string keys = Path.Combine(Path.GetTempPath(), "variable-test-keys-" + Guid.NewGuid().ToString("N"));
    private readonly TestClock clock = new();
    private readonly string operatorConnection = Environment.GetEnvironmentVariable("VARIABLE_TEST_OPERATOR_CONNECTION")
        ?? "Host=localhost;Port=54637;Database=postgres;Username=postgres;Password=variable-local-operator-only";
    private WebApplicationFactory<Program> factory = null!;
    private string RuntimeConnection => Connection("variable_runtime", "variable-local-runtime-only");
    private string MigrationConnection => Connection("variable_migrator", "variable-local-migrator-only");
    private string OwnerConnection => new NpgsqlConnectionStringBuilder(operatorConnection) { Database = database }.ConnectionString;

    private string Connection(string username, string password) => new NpgsqlConnectionStringBuilder(operatorConnection)
        { Database = database, Username = username, Password = password }.ConnectionString;

    public async Task InitializeAsync()
    {
        await using (var connection = new NpgsqlConnection(operatorConnection))
        {
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE {database} OWNER variable_migrator", connection);
            await create.ExecuteNonQueryAsync();
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:Migration"] = MigrationConnection, ["Database:RuntimeRole"] = "variable_runtime" }).Build();
        await IdentityModule.RunMigrationAsync(configuration);
        factory = CreateFactory();
    }

    private WebApplicationFactory<Program> CreateFactory(string? runtime = null, Guid? configuredOrganization = null) => new IdentityFactory(new Dictionary<string, string?>
        {
            [HostDefaults.EnvironmentKey] = "Testing",
            ["ConnectionStrings:Runtime"] = runtime ?? RuntimeConnection,
            ["ConnectionStrings:Migration"] = null,
            ["Installation:OrganizationId"] = (configuredOrganization ?? organization).ToString(),
            ["Installation:PublicOrigin"] = "https://localhost",
            ["DataProtection:KeyDirectory"] = keys
        }, clock);

    private sealed class IdentityFactory(Dictionary<string, string?> configuration, TimeProvider clock) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Host configuration is available during CreateBuilder, before the module validates it.
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(configuration));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureServices(services => services.AddSingleton(clock));
    }

    public async Task DisposeAsync()
    {
        await factory.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(operatorConnection);
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
        if (Directory.Exists(keys)) Directory.Delete(keys, true);
    }

    private Task Bootstrap() => IdentityModule.RunBootstrapAsync(factory.Services, "Synthetic Training", "Test Administrator", Email, Password);
    private HttpClient Client(WebApplicationFactory<Program>? source = null) => (source ?? factory).CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, object payload)
    {
        using var csrf = await client.GetAsync("/api/auth/csrf");
        csrf.EnsureSuccessStatusCode();
        var token = (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string email = Email, string password = Password) =>
        Post(client, "/api/auth/login", new { email, password });
    private static async Task<Guid> UserId(HttpClient client) => (await client.GetFromJsonAsync<SessionView>("/api/auth/session"))!.Account.Id;
    private async Task<long> Count(string sql)
    {
        await using var connection = new NpgsqlConnection(OwnerConnection); await connection.OpenAsync();
        await using var query = new NpgsqlCommand(sql, connection); return (long)(await query.ExecuteScalarAsync())!;
    }
    private async Task Execute(string sql)
    {
        await using var connection = new NpgsqlConnection(OwnerConnection); await connection.OpenAsync();
        await using var query = new NpgsqlCommand(sql, connection); await query.ExecuteNonQueryAsync();
    }
    private async Task<Guid> Seed(string email, string[] roles, Guid? org = null)
    {
        var id = Guid.NewGuid(); var organizationId = org ?? organization;
        await using var connection = new NpgsqlConnection(OwnerConnection); await connection.OpenAsync();
        await using var organizationInsert = new NpgsqlCommand("INSERT INTO identity.organizations(id, name, created_at) VALUES ($1, 'Synthetic organization', now()) ON CONFLICT DO NOTHING", connection);
        organizationInsert.Parameters.AddWithValue(organizationId);
        await organizationInsert.ExecuteNonQueryAsync();
        await using var query = new NpgsqlCommand("""
            INSERT INTO identity.users(id, organization_id, name, email, normalized_email, password_hash, roles, status, security_version, created_at, updated_at)
            VALUES ($2, $1, 'Synthetic account', $3, upper($3), $4, $5, 'active', 1, now(), now());
            """, connection);
        query.Parameters.AddWithValue(organizationId); query.Parameters.AddWithValue(id); query.Parameters.AddWithValue(email);
        query.Parameters.AddWithValue(new PasswordHasher<object>().HashPassword(new object(), Password)); query.Parameters.AddWithValue(roles);
        await query.ExecuteNonQueryAsync(); return id;
    }

    [Fact]
    public async Task Concurrent_bootstrap_creates_one_staff_administrator_and_atomic_audit()
    {
        _ = factory.Services;
        var outcomes = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            try { await Bootstrap(); return "ok"; } catch (Exception error) { return error.Message; }
        }));
        Assert.Single(outcomes, outcome => outcome == "ok");
        Assert.Single(outcomes, outcome => outcome == "already_bootstrapped");
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.installation"));
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.users"));
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.users WHERE status = 'active' AND 'Learner' = ANY(roles)"));
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.audit_log WHERE action = 'organization.bootstrapped'"));
    }

    [Fact]
    public async Task Login_requires_csrf_rejects_role_injection_and_uses_secure_http_only_cookie()
    {
        await Bootstrap(); using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/bootstrap", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/auth/login", new { email = Email, password = Password, roles = new[] { "Administrator" } })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, password: "Wrong-password-123!")).StatusCode);
        using var response = await Login(client); Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("__Host-variable-session=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase); Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(organization, (await client.GetFromJsonAsync<SessionView>("/api/auth/session"))!.Organization.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(client, "/api/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.sessions"));
    }

    [Fact]
    public async Task Organization_context_cannot_be_overridden_and_foreign_targets_are_hidden()
    {
        await Bootstrap(); var foreign = await Seed("other@example.test", ["Administrator"], Guid.NewGuid());
        using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "other@example.test")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Login(client)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Organization-Id", Guid.NewGuid().ToString());
        Assert.Equal(organization, (await client.GetFromJsonAsync<SessionView>("/api/auth/session"))!.Organization.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(client, $"/api/administration/users/{foreign}/deactivate", new { currentPassword = Password, reason = "Test" })).StatusCode);
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.users WHERE status = 'deactivated'"));
    }

    [Fact]
    public async Task Manager_cannot_promote_self_or_deactivate_privileged_accounts()
    {
        await Bootstrap(); var managerId = await Seed("manager@example.test", ["OrganizationManager"]);
        using var administrator = Client(); await Login(administrator); var adminId = await UserId(administrator);
        using var manager = Client(); await Login(manager, "manager@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(manager, $"/api/administration/users/{managerId}/administrator-role", new { granted = true, currentPassword = Password, reason = "Test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(manager, $"/api/administration/users/{adminId}/deactivate", new { currentPassword = Password, reason = "Test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(manager, $"/api/administration/users/{managerId}/deactivate", new { reason = "Test" })).StatusCode);
    }

    [Fact]
    public async Task Final_administrator_is_protected_and_competing_revocations_serialize()
    {
        await Bootstrap(); using var first = Client(); await Login(first); var firstId = await UserId(first);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(first, $"/api/administration/users/{firstId}/administrator-role", new { currentPassword = Password, reason = "Missing explicit grant decision" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(first, $"/api/administration/users/{firstId}/administrator-role", new { granted = false, currentPassword = Password, reason = "Test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(first, $"/api/administration/users/{firstId}/administrator-role", new { granted = false, currentPassword = "Wrong-password", reason = "Test" })).StatusCode);
        var secondId = await Seed("second@example.test", ["Administrator"]);
        using var second = Client(); await Login(second, "second@example.test");
        var responses = await Task.WhenAll(
            Post(first, $"/api/administration/users/{secondId}/administrator-role", new { granted = false, currentPassword = Password, reason = "Concurrent test" }),
            Post(second, $"/api/administration/users/{firstId}/administrator-role", new { granted = false, currentPassword = Password, reason = "Concurrent test" }));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.All(responses, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NoContent, HttpStatusCode.Unauthorized }));
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.users WHERE status = 'active' AND 'Administrator' = ANY(roles)"));
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.audit_log WHERE action = 'administrator.revoked'"));
    }

    [Fact]
    public async Task Deactivation_revokes_existing_sessions_without_removing_accounts()
    {
        await Bootstrap(); var learner = await Seed("learner@example.test", ["Learner"]);
        using var userClient = Client(); await Login(userClient, "learner@example.test");
        using var admin = Client(); await Login(admin);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(admin, $"/api/administration/users/{learner}/deactivate", new { reason = "Synthetic departure" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await userClient.GetAsync("/api/auth/session")).StatusCode);
        Assert.Equal(2, await Count("SELECT count(*) FROM identity.users"));
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.users WHERE status = 'active' AND 'Learner' = ANY(roles)"));
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.audit_log WHERE action = 'user.deactivated'"));
    }

    [Fact]
    public async Task Audit_failure_rolls_back_deactivation_and_session_deletion()
    {
        await Bootstrap(); var target = await Seed("author@example.test", ["CourseAuthor"]);
        using var author = Client(); await Login(author, "author@example.test");
        using var admin = Client(); await Login(admin);
        await Execute("REVOKE INSERT ON identity.audit_log FROM variable_runtime");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Post(admin, $"/api/administration/users/{target}/deactivate", new { reason = "Audit failure test" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await author.GetAsync("/api/auth/session")).StatusCode);
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.users WHERE status = 'deactivated'"));
    }

    [Fact]
    public async Task Runtime_cannot_run_ddl_or_mutate_audit_and_incompatible_schema_fails_readiness()
    {
        await Bootstrap();
        await using var connection = new NpgsqlConnection(RuntimeConnection); await connection.OpenAsync();
        foreach (var sql in new[] { "CREATE TABLE identity.forbidden(id integer)", "DELETE FROM identity.audit_log", "DELETE FROM identity.users", "DELETE FROM identity.installation" })
        {
            await using var query = new NpgsqlCommand(sql, connection);
            var error = await Assert.ThrowsAsync<PostgresException>(() => query.ExecuteNonQueryAsync()); Assert.Equal("42501", error.SqlState);
        }
        using var client = Client(); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        await Execute("UPDATE platform.schema_migrations SET version = 999");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/auth/csrf")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }

    [Fact]
    public async Task Sessions_and_protected_keys_survive_a_host_restart()
    {
        await Bootstrap(); using var first = Client(); using var login = await Login(first);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith("__Host-variable-session=", StringComparison.Ordinal)).Split(';')[0];
        await using var restarted = CreateFactory(); using var second = Client(restarted);
        second.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync("/api/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Temporary_lockout_recovers_without_removing_the_final_administrator()
    {
        await Bootstrap(); using var client = Client();
        for (var attempt = 0; attempt < 5; attempt++) Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, password: "Wrong-password")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client)).StatusCode);
        clock.Advance(TimeSpan.FromMinutes(16));
        Assert.Equal(HttpStatusCode.NoContent, (await Login(client)).StatusCode);
    }

    [Fact]
    public async Task Migration_is_repeatable_and_rejects_modified_history()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:Migration"] = MigrationConnection, ["Database:RuntimeRole"] = "variable_runtime" }).Build();
        await Task.WhenAll(IdentityModule.RunMigrationAsync(configuration), IdentityModule.RunMigrationAsync(configuration));
        Assert.Equal(1, await Count("SELECT count(*) FROM platform.schema_migrations"));
        await Execute("UPDATE platform.schema_migrations SET sha256 = repeat('0', 64)");
        await Assert.ThrowsAsync<InvalidOperationException>(() => IdentityModule.RunMigrationAsync(configuration));
    }

    [Fact]
    public async Task Privileged_database_identity_is_not_a_valid_runtime()
    {
        await using var unsafeHost = CreateFactory(MigrationConnection); using var client = Client(unsafeHost);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Administrator_grant_requires_reauthentication_and_invalidates_existing_sessions()
    {
        await Bootstrap(); var author = await Seed("author@example.test", ["CourseAuthor"]);
        using var staff = Client(); await Login(staff, "author@example.test");
        using var admin = Client(); await Login(admin);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(admin, $"/api/administration/users/{author}/administrator-role",
            new { granted = true, currentPassword = "wrong", reason = "Synthetic promotion" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(admin, $"/api/administration/users/{author}/administrator-role",
            new { granted = true, currentPassword = Password, reason = "Synthetic promotion" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staff.GetAsync("/api/auth/session")).StatusCode);
        await Login(staff, "author@example.test");
        Assert.Contains("Administrator", (await staff.GetFromJsonAsync<SessionView>("/api/auth/session"))!.Account.Roles);
        Assert.Equal(1, await Count("SELECT count(*) FROM identity.audit_log WHERE action = 'administrator.granted'"));
    }

    [Fact]
    public async Task Stale_security_version_and_expired_sessions_are_rejected_even_if_rows_remain()
    {
        await Bootstrap(); using var client = Client(); await Login(client);
        await Execute("UPDATE identity.users SET security_version = security_version + 1");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
        await Login(client);
        clock.Advance(TimeSpan.FromHours(9));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Bootstrap_rolls_back_all_state_when_required_audit_cannot_be_persisted()
    {
        await Execute("REVOKE INSERT ON identity.audit_log FROM variable_runtime");
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => Bootstrap());
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.organizations"));
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.users"));
        Assert.Equal(0, await Count("SELECT count(*) FROM identity.installation"));
    }

    [Fact]
    public async Task Changing_the_configured_organization_after_bootstrap_fails_readiness()
    {
        await Bootstrap();
        await using var wrongOrganization = CreateFactory(configuredOrganization: Guid.NewGuid());
        using var client = Client(wrongOrganization);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/auth/csrf")).StatusCode);
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        internal void Advance(TimeSpan duration) => now += duration;
    }
}

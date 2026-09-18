using Variable.Authoring;
using Variable.Database;
using Variable.Identity;

namespace Variable.App;

public static class ApplicationMigrations
{
    public static MigrationPlan Plan => new(IdentityModule.Migration, AuthoringModule.Migration);
    public static Task RunAsync(IConfiguration configuration) => Plan.ApplyAsync(
        configuration.GetConnectionString("Migration") ?? throw new InvalidOperationException("ConnectionStrings:Migration is required for migrate."),
        configuration["Database:RuntimeRole"] ?? throw new InvalidOperationException("Database:RuntimeRole is required for migrate."));
}

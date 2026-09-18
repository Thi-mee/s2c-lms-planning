using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Variable.Identity.Persistence;
using Variable.Database;

namespace Variable.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddVariableIdentity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var installation = InstallationOptions.Read(configuration, environment.IsDevelopment());
        services.AddSingleton(installation);
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<IdentityDb>(options => options.UseNpgsql(installation.ConnectionString));
        services.AddScoped<IdentityService>();
        services.AddScoped<IIdentityAccess, IdentityAccess>();
        services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        var protection = services.AddDataProtection().SetApplicationName("Variable.LMS")
            .PersistKeysToFileSystem(new DirectoryInfo(installation.KeyDirectory));
        var certificateFile = configuration["DataProtection:CertificatePath"];
        if (!string.IsNullOrWhiteSpace(certificateFile))
        {
            var passwordFile = configuration["DataProtection:CertificatePasswordFile"];
            var password = passwordFile is null ? null : File.ReadAllText(passwordFile).TrimEnd('\r', '\n');
            protection.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(certificateFile, password, X509KeyStorageFlags.EphemeralKeySet));
        }
        else if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("DataProtection:CertificatePath is required outside local development/tests.");

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = installation.Development ? "variable-dev-csrf" : "__Host-variable-csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = installation.Development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddSingleton<PostgresTicketStore>();
        services.AddAuthentication(IdentityClaims.Scheme).AddCookie(IdentityClaims.Scheme, options =>
        {
            options.Cookie.Name = installation.Development ? "variable-dev-session" : "__Host-variable-session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = installation.Development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        services.AddOptions<CookieAuthenticationOptions>(IdentityClaims.Scheme)
            .Configure<PostgresTicketStore>((options, tickets) => options.SessionStore = tickets);
        services.AddAuthorization();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("identity", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("security", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        return services;
    }

    public static ModuleMigration Migration => ModuleMigration.Embedded(typeof(IdentityModule).Assembly, 1, "0001-identity", "identity", """
        GRANT USAGE ON SCHEMA identity TO {runtime_role};
        REVOKE ALL ON ALL TABLES IN SCHEMA identity FROM {runtime_role};
        GRANT SELECT, INSERT, UPDATE ON identity.organizations, identity.users TO {runtime_role};
        GRANT SELECT, INSERT ON identity.installation, identity.audit_log TO {runtime_role};
        GRANT SELECT, INSERT, UPDATE, DELETE ON identity.sessions TO {runtime_role};
        """);

    public static async Task RunBootstrapAsync(IServiceProvider services, string organizationName, string administratorName, string email, string password)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IdentityService>().BootstrapAsync(organizationName, administratorName, email, password);
    }

    public static IApplicationBuilder UseVariableIdentityErrors(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        try
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.Headers.CacheControl = "no-store";
                if (!await DatabaseLifecycle.ReadyAsync(context.RequestServices.GetRequiredService<InstallationOptions>(),
                    context.RequestServices.GetRequiredService<MigrationPlan>(), context.RequestAborted))
                { await WriteErrorAsync(context, 503, "database_not_ready"); return; }
            }
            await next();
        }
        catch (IdentityFailure error) { await WriteErrorAsync(context, error.Status, error.Code); }
        catch (AntiforgeryValidationException) { await WriteErrorAsync(context, 400, "invalid_csrf_token"); }
        catch (Exception error) when (error is NpgsqlException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Variable.Persistence")
                .LogError("Persistence operation failed ({ErrorType}); request {RequestId}", error.GetType().Name, context.TraceIdentifier);
            await WriteErrorAsync(context, 503, "persistence_unavailable");
        }
    });

    private static Task WriteErrorAsync(HttpContext context, int status, string code)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { code, requestId = context.TraceIdentifier });
    }

    public static IEndpointRouteBuilder MapVariableIdentity(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
        endpoints.MapGet("/health/ready", async (InstallationOptions installation, MigrationPlan migrations, CancellationToken ct) =>
        {
            try { return await DatabaseLifecycle.ReadyAsync(installation, migrations, ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503); }
            catch (NpgsqlException) { return Results.StatusCode(503); }
        });

        var api = endpoints.MapGroup("/api");
        api.MapGet("/auth/csrf", (IAntiforgery antiforgery, HttpContext context) =>
            Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }));
        api.MapPost("/auth/login", async (LoginRequest request, IAntiforgery antiforgery, IdentityService identity, HttpContext context) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var principal = await identity.AuthenticateAsync(request.Email, request.Password, context.RequestAborted);
            await context.SignInAsync(IdentityClaims.Scheme, principal, new AuthenticationProperties { IsPersistent = false });
            return Results.NoContent();
        }).RequireRateLimiting("identity");
        api.MapGet("/auth/session", async (IdentityService identity, HttpContext context) =>
            Results.Ok(await identity.CurrentAsync(context.User, context.RequestAborted))).RequireAuthorization();
        api.MapPost("/auth/logout", async (IAntiforgery antiforgery, HttpContext context) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            await context.SignOutAsync(IdentityClaims.Scheme);
            return Results.NoContent();
        }).RequireAuthorization();
        api.MapPost("/administration/users/{id:guid}/administrator-role", async (Guid id, AdministratorRequest request,
            IAntiforgery antiforgery, IdentityService identity, HttpContext context) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            await identity.ChangeAdministratorAsync(context.User, id, request.Granted, request.CurrentPassword, request.Reason, context.RequestAborted);
            if (id == IdentityClaims.UserId(context.User) && !request.Granted) await context.SignOutAsync(IdentityClaims.Scheme);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("security");
        api.MapPost("/administration/users/{id:guid}/deactivate", async (Guid id, DeactivationRequest request,
            IAntiforgery antiforgery, IdentityService identity, HttpContext context) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            await identity.DeactivateAsync(context.User, id, request.CurrentPassword, request.Reason, context.RequestAborted);
            if (id == IdentityClaims.UserId(context.User)) await context.SignOutAsync(IdentityClaims.Scheme);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("security");
        api.MapGet("/administration/users", async (string? search, IdentityService identity, HttpContext context) =>
            Results.Ok(await identity.FindAccountsAsync(context.User, (search ?? "")[..Math.Min(search?.Length ?? 0, 320)], context.RequestAborted))).RequireAuthorization();
        api.MapPost("/administration/users/{id:guid}/course-author-role", async (Guid id, CourseAuthorRequest request,
            IAntiforgery antiforgery, IdentityService identity, HttpContext context) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            await identity.ChangeCourseAuthorAsync(context.User, id, request.Granted, request.Reason, request.CurrentPassword, context.RequestAborted);
            if (id == IdentityClaims.UserId(context.User)) await context.SignOutAsync(IdentityClaims.Scheme);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("security");
        api.MapPost("/administration/users/{id:guid}/cohort-coordinator-role", async (Guid id, OrdinaryRoleRequest request,
            IAntiforgery antiforgery, IdentityService identity, HttpContext context) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            await identity.ChangeOrdinaryRoleAsync(context.User, id, AccountRole.CohortCoordinator, request.Granted, request.Reason,
                request.CurrentPassword, context.RequestAborted);
            if (id == IdentityClaims.UserId(context.User)) await context.SignOutAsync(IdentityClaims.Scheme);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("security");
        api.MapPost("/administration/users/{id:guid}/learning-facilitator-role", async (Guid id, OrdinaryRoleRequest request,
            IAntiforgery antiforgery, IdentityService identity, HttpContext context) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            await identity.ChangeOrdinaryRoleAsync(context.User, id, AccountRole.LearningFacilitator, request.Granted, request.Reason,
                request.CurrentPassword, context.RequestAborted);
            if (id == IdentityClaims.UserId(context.User)) await context.SignOutAsync(IdentityClaims.Scheme);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("security");
        return endpoints;
    }

    private sealed record CourseAuthorRequest(bool Granted, string Reason, string? CurrentPassword = null);
    private sealed record OrdinaryRoleRequest(bool Granted, string Reason, string? CurrentPassword = null);
    private sealed record LoginRequest(string Email, string Password);
    private sealed record AdministratorRequest(bool Granted, string CurrentPassword, string Reason);
    private sealed record DeactivationRequest(string Reason, string? CurrentPassword = null);
}

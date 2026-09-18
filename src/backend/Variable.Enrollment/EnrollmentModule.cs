using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Variable.Database;
using Variable.Identity;

namespace Variable.Enrollment;

public static class EnrollmentModule
{
    public static ModuleMigration Migration => ModuleMigration.Embedded(typeof(EnrollmentModule).Assembly, 3, "0003-enrollment", "enrollment", """
        GRANT USAGE ON SCHEMA enrollment TO {runtime_role};
        GRANT SELECT, INSERT, UPDATE ON enrollment.cohorts TO {runtime_role};
        GRANT SELECT, INSERT, DELETE ON enrollment.cohort_staff TO {runtime_role};
        """);

    public static IServiceCollection AddVariableEnrollment(this IServiceCollection services) => services
        .AddScoped<EnrollmentService>()
        .AddScoped<IAccountAccessRemovalGuard, CohortAccessRemovalGuard>();

    public static IEndpointRouteBuilder MapVariableEnrollment(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/cohorts").RequireAuthorization().AddEndpointFilter(async (context, next) =>
        {
            try
            {
                if (context.HttpContext.Request.Method != "GET")
                    await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
                return await next(context);
            }
            catch (EnrollmentFailure failure)
            { return Results.Json(new { code = failure.Code, requestId = context.HttpContext.TraceIdentifier }, statusCode: failure.Status); }
        });
        api.MapGet("", async (int? offset, EnrollmentService service, HttpContext context) =>
            Results.Ok(await service.ListAsync(context.User, offset ?? 0, context.RequestAborted)));
        api.MapGet("/eligible-staff", async (string capability, string? search, IIdentityAccess identity, HttpContext context) =>
            Results.Ok(await identity.EligibleCohortStaffAsync(context.User, capability, (search ?? "")[..Math.Min(search?.Length ?? 0, 320)], context.RequestAborted)));
        api.MapGet("/{id:guid}", async (Guid id, EnrollmentService service, HttpContext context) =>
            Results.Ok(await service.GetAsync(context.User, id, context.RequestAborted)));
        api.MapPost("", async (CreateCohort request, EnrollmentService service, HttpContext context) =>
            Results.Ok(await service.CreateAsync(context.User, request, context.RequestAborted)));
        api.MapPost("/{id:guid}/staff", async (Guid id, ChangeCohortStaff request, EnrollmentService service, HttpContext context) =>
            Results.Ok(await service.ChangeStaffAsync(context.User, id, request, context.RequestAborted)));
        return endpoints;
    }
}

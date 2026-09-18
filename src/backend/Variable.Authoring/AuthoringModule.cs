using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Variable.Database;
using Variable.Identity;

namespace Variable.Authoring;

public static class AuthoringModule
{
    public static ModuleMigration Migration => ModuleMigration.Embedded(typeof(AuthoringModule).Assembly, 2, "0002-authoring", "authoring", """
        GRANT USAGE ON SCHEMA authoring TO {runtime_role};
        GRANT SELECT, INSERT, UPDATE ON authoring.courses, authoring.course_authors TO {runtime_role};
        GRANT SELECT, INSERT, UPDATE, DELETE ON authoring.modules, authoring.lessons TO {runtime_role};
        """);

    public static IServiceCollection AddVariableAuthoring(this IServiceCollection services) => services
        .AddScoped<AuthoringService>()
        .AddScoped<IAuthoringAccess, AuthoringAccess>();

    public static IEndpointRouteBuilder MapVariableAuthoring(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/authoring").RequireAuthorization().AddEndpointFilter(async (context, next) =>
        {
            try
            {
                if (context.HttpContext.Request.Method != "GET")
                    await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
                return await next(context);
            }
            catch (AuthoringFailure failure)
            { return Results.Json(new { code = failure.Code, requestId = context.HttpContext.TraceIdentifier }, statusCode: failure.Status); }
        });
        api.MapGet("/courses", async (int? offset, AuthoringService service, HttpContext context) =>
            Results.Ok(await service.ListAsync(context.User, offset ?? 0, context.RequestAborted)));
        api.MapGet("/owners", async (string? search, IIdentityAccess identity, HttpContext context) =>
            Results.Ok(await identity.EligibleOwnersAsync(context.User, (search ?? "")[..Math.Min(search?.Length ?? 0, 320)], context.RequestAborted)));
        api.MapGet("/courses/{id:guid}", async (Guid id, AuthoringService service, HttpContext context) =>
            Results.Ok(await service.GetAsync(context.User, id, context.RequestAborted)));
        api.MapPost("/courses", async (CreateCourse request, AuthoringService service, HttpContext context) =>
            Results.Ok(await service.CreateAsync(context.User, request, context.RequestAborted)));
        api.MapPost("/courses/{id:guid}/draft", async (Guid id, SaveCourse request, AuthoringService service, HttpContext context) =>
            Results.Ok(await service.SaveAsync(context.User, id, request, context.RequestAborted)));
        api.MapPost("/courses/{id:guid}/publish", async (Guid id, PublishCourse request, AuthoringService service, HttpContext context) =>
            Results.Ok(await service.PublishAsync(context.User, id, request.ExpectedRevision, context.RequestAborted)));
        api.MapPost("/courses/{id:guid}/owner", async (Guid id, ChangeOwner request, AuthoringService service, HttpContext context) =>
            Results.Ok(await service.ChangeOwnerAsync(context.User, id, request, context.RequestAborted)));
        return endpoints;
    }
}

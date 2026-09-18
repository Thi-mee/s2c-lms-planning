using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Variable.Identity;
using Variable.App;
using Variable.Authoring;
using Variable.Enrollment;
using Variable.Licensing;
using Variable.Notifications;

var command = args.FirstOrDefault();
var builder = WebApplication.CreateBuilder(command is "migrate" or "bootstrap" ? args[1..] : args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_000_000);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});

if (args.FirstOrDefault() == "migrate")
{
    await ApplicationMigrations.RunAsync(builder.Configuration);
    Console.WriteLine("Database migrations applied successfully.");
    return;
}

builder.Services.AddSingleton(ApplicationMigrations.Plan);
builder.Services.AddVariableIdentity(builder.Configuration, builder.Environment);
builder.Services.AddVariableLicensing(builder.Configuration);
builder.Services.AddVariableNotifications(builder.Configuration, builder.Environment);
builder.Services.AddVariableAuthoring();
builder.Services.AddVariableEnrollment();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    foreach (var proxy in builder.Configuration.GetSection("Security:TrustedProxies").Get<string[]>() ?? [])
        options.KnownProxies.Add(IPAddress.Parse(proxy));
});
var app = builder.Build();
if (args.FirstOrDefault() == "bootstrap")
{
    var passwordFile = builder.Configuration["Bootstrap:PasswordFile"]
        ?? throw new InvalidOperationException("Bootstrap:PasswordFile is required; do not pass passwords as CLI arguments.");
    await IdentityModule.RunBootstrapAsync(app.Services,
        builder.Configuration["Bootstrap:OrganizationName"] ?? "",
        builder.Configuration["Bootstrap:AdministratorName"] ?? "",
        builder.Configuration["Bootstrap:Email"] ?? "",
        (await File.ReadAllTextAsync(passwordFile)).TrimEnd('\r', '\n'));
    Console.WriteLine("Organization and first staff-only Administrator established. Remove the bootstrap password file.");
    return;
}
if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
    throw new InvalidOperationException("Supported commands: serve (no command), migrate, bootstrap.");

if (builder.Configuration.GetSection("Security:TrustedProxies").GetChildren().Any()) app.UseForwardedHeaders();
app.UseVariableIdentityErrors();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing") && !context.Request.IsHttps && !context.Request.Path.StartsWithSegments("/health"))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { code = "https_required" });
        return;
    }
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapVariableIdentity();
app.MapVariableLicensing();
app.MapVariableAuthoring();
app.MapVariableEnrollment();
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/health") || context.Request.Method != "GET")
    { context.Response.StatusCode = 404; return; }
    var index = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
    if (!File.Exists(index)) { context.Response.StatusCode = 404; return; }
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(index);
});
app.Run();

public partial class Program;

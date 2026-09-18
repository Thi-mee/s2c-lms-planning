using Microsoft.Extensions.Configuration;

namespace Variable.Identity;

internal sealed record InstallationOptions(Guid OrganizationId, string ConnectionString, string KeyDirectory, bool Development)
{
    internal static InstallationOptions Read(IConfiguration configuration, bool development)
    {
        if (!Guid.TryParse(configuration["Installation:OrganizationId"], out var organizationId) || organizationId == Guid.Empty)
            throw new InvalidOperationException("Installation:OrganizationId must be a nonempty UUID.");
        var connection = configuration.GetConnectionString("Runtime");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("ConnectionStrings:Runtime is required.");
        if (!string.IsNullOrEmpty(configuration.GetConnectionString("Migration")))
            throw new InvalidOperationException("Migration credentials must not be supplied to the normal runtime or bootstrap command.");
        var keys = configuration["DataProtection:KeyDirectory"];
        if (string.IsNullOrWhiteSpace(keys) || !Path.IsPathFullyQualified(keys))
            throw new InvalidOperationException("DataProtection:KeyDirectory must be an absolute persistent directory.");
        if (!Uri.TryCreate(configuration["Installation:PublicOrigin"], UriKind.Absolute, out var origin)
            || origin.Scheme is not ("https" or "http") || (!development && origin.Scheme != "https")
            || (development && origin.Scheme == "http" && !origin.IsLoopback)
            || origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment) || !string.IsNullOrEmpty(origin.UserInfo))
            throw new InvalidOperationException("Installation:PublicOrigin must be an origin URL (HTTPS outside Development).");
        return new(organizationId, connection, keys, development);
    }
}

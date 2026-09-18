using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Variable.Identity.Persistence;

namespace Variable.Identity;

internal sealed class PostgresTicketStore(IServiceScopeFactory scopes, InstallationOptions installation, IDataProtectionProvider protection, TimeProvider clock) : ITicketStore
{
    private readonly IDataProtector protector = protection.CreateProtector("Variable.Identity.Session.v1");
    private static string Hash(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDb>();
        var userId = IdentityClaims.UserId(ticket.Principal);
        var version = IdentityClaims.SecurityVersion(ticket.Principal);
        if (!await db.Users.AnyAsync(x => x.OrganizationId == installation.OrganizationId && x.Id == userId
                && x.Status == "active" && x.DeletedAt == null && x.SecurityVersion == version))
            throw new IdentityFailure("session_changed", 401);
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.Sessions.Add(new StoredSession { KeyHash = Hash(key), OrganizationId = installation.OrganizationId, UserId = userId,
            SecurityVersion = version, Ticket = protector.Protect(TicketSerializer.Default.Serialize(ticket)),
            ExpiresAt = ticket.Properties.ExpiresUtc ?? clock.GetUtcNow().AddHours(8) });
        await db.SaveChangesAsync();
        return key;
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (key.Length != 64) return null;
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDb>();
        var hash = Hash(key);
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == installation.OrganizationId && x.KeyHash == hash);
        if (session is null || session.ExpiresAt <= clock.GetUtcNow()) return null;
        if (!await db.Users.AnyAsync(x => x.OrganizationId == installation.OrganizationId && x.Id == session.UserId
                && x.Status == "active" && x.DeletedAt == null && x.SecurityVersion == session.SecurityVersion)) return null;
        try
        {
            var ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(session.Ticket));
            return ticket?.Principal.FindFirstValue(IdentityClaims.Organization) == installation.OrganizationId.ToString() ? ticket : null;
        }
        catch (CryptographicException) { return null; }
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        // No sliding expiry. Updating an existing row must never recreate a revoked session.
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDb>();
        var hash = Hash(key);
        var bytes = protector.Protect(TicketSerializer.Default.Serialize(ticket));
        await db.Sessions.Where(x => x.OrganizationId == installation.OrganizationId && x.KeyHash == hash)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Ticket, bytes));
    }

    public async Task RemoveAsync(string key)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDb>();
        var hash = Hash(key);
        await db.Sessions.Where(x => x.OrganizationId == installation.OrganizationId && x.KeyHash == hash).ExecuteDeleteAsync();
    }
}

using Microsoft.EntityFrameworkCore;

namespace Variable.Identity.Persistence;

internal sealed class IdentityDb(DbContextOptions<IdentityDb> options) : DbContext(options)
{
    internal DbSet<Organization> Organizations => Set<Organization>();
    internal DbSet<UserAccount> Users => Set<UserAccount>();
    internal DbSet<StoredSession> Sessions => Set<StoredSession>();
    internal DbSet<AuditEntry> Audit => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Organization>().ToTable("organizations", "identity").HasKey(x => x.Id);
        model.Entity<UserAccount>().ToTable("users", "identity").HasKey(x => x.Id);
        model.Entity<UserAccount>().HasAlternateKey(x => new { x.OrganizationId, x.Id });
        model.Entity<UserAccount>().HasIndex(x => new { x.OrganizationId, x.NormalizedEmail }).IsUnique();
        model.Entity<UserAccount>().HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<StoredSession>().ToTable("sessions", "identity").HasKey(x => x.KeyHash);
        model.Entity<StoredSession>().HasOne<UserAccount>().WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.UserId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AuditEntry>().ToTable("audit_log", "identity").HasKey(x => x.Id);
        model.Entity<AuditEntry>().Property(x => x.Metadata).HasColumnType("jsonb");
        model.Entity<AuditEntry>().HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<AuditEntry>().HasOne<UserAccount>().WithMany()
            .HasForeignKey(x => new { x.OrganizationId, x.ActorUserId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        foreach (var entity in model.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
            property.SetColumnName(System.Text.RegularExpressions.Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}

internal sealed class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class UserAccount
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Status { get; set; } = "active";
    public string[] Roles { get; set; } = [];
    public long SecurityVersion { get; set; } = 1;
    public int FailedAccessCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    internal bool Active => Status == "active" && DeletedAt is null;
    internal bool Administrator => Roles.Contains(nameof(AccountRole.Administrator));
    internal AccountView View() => new(Id, Name, Email, Roles);
}

internal sealed class StoredSession
{
    public string KeyHash { get; set; } = "";
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public long SecurityVersion { get; set; }
    public byte[] Ticket { get; set; } = [];
    public DateTimeOffset ExpiresAt { get; set; }
}

internal sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorKind { get; set; } = "user";
    public string Action { get; set; } = "";
    public Guid TargetId { get; set; }
    public string Metadata { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}

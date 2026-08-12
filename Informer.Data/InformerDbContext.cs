using Informer.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Informer.Data;

/// <summary>
/// EF Core context. Deliberately provider-agnostic: the concrete provider (SQLite by
/// default, but swappable to SQL Server / PostgreSQL / etc.) is configured by the caller
/// via <see cref="DbContextOptions{TContext}"/> in Program.cs — nothing here references
/// a specific provider, so "use any DB through EF Core" (requirement) is satisfied.
/// </summary>
public class InformerDbContext : DbContext
{
    public InformerDbContext(DbContextOptions<InformerDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<ApiKeyEntity> ApiKeys => Set<ApiKeyEntity>();

    public DbSet<AppSettingsEntity> AppSettings => Set<AppSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Sender).IsRequired().HasMaxLength(256);
            e.Property(n => n.Description).HasMaxLength(2000);
            e.Property(n => n.ResponseBodyJson).IsRequired();
            // Sender is queried/filtered constantly from the history view -> index it.
            e.HasIndex(n => n.Sender);
            e.HasIndex(n => n.CreatedAtUtc);
            e.Property(n => n.Severity).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<ApiKeyEntity>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.Key).IsRequired().HasMaxLength(256);
            e.HasIndex(k => k.Key).IsUnique();
        });

        modelBuilder.Entity<AppSettingsEntity>(e =>
        {
            e.HasKey(s => s.Id);
            // Single-row settings table, always Id = 1.
            e.HasData(new AppSettingsEntity
            {
                Id = 1,
                RetentionDays = 30,
                RequireApiKey = true,
                ToastDisplaySeconds = 8,
                RateLimitMaxRequests = 20,
                RateLimitWindowSeconds = 10,
                ListenPort = 5005,
                ShowInfoToasts = true,
                ShowWarningToasts = true,
                ShowErrorToasts = true
            });
        });
    }
}

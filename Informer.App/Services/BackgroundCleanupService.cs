using System;
using System.Threading;
using System.Threading.Tasks;
using Informer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Informer.App.Services;

public class BackgroundCleanupService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private readonly IServiceProvider _services;

    public BackgroundCleanupService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await PurgeOldNotificationsAsync(token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                using var scope = _services.CreateScope();
                var logger = scope.ServiceProvider.GetService<ILogger<BackgroundCleanupService>>();
                logger?.LogError(ex, "Background cleanup pass failed");
            }

            try
            {
                await Task.Delay(CheckInterval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PurgeOldNotificationsAsync(CancellationToken token)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(token);
        var retentionDays = settings?.RetentionDays ?? 30;
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        // EF Core 6 has no ExecuteDelete; batch-load ids and RemoveRange instead. Notification
        // rows are small (id, sender, description, json) so loading a day's worth of expired
        // rows into memory before deleting them is not a concern in practice.
        var toDelete = await db.Notifications
            .Where(n => n.CreatedAtUtc < cutoff)
            .ToListAsync(token);

        if (toDelete.Count > 0)
        {
            db.Notifications.RemoveRange(toDelete);
            await db.SaveChangesAsync(token);
        }

        var deleted = toDelete.Count;
        if (deleted > 0)
        {
            using var innerScope = _services.CreateScope();
            var logger = innerScope.ServiceProvider.GetService<ILogger<BackgroundCleanupService>>();
            logger?.LogInformation("Cleanup purged {Count} notification(s) older than {Days} day(s)", deleted, retentionDays);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

public class NotificationCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationCleanupWorker> _logger;
    private readonly TimeSpan _interval;
    private readonly int _retentionDays;

    public NotificationCleanupWorker(IServiceProvider services, IConfiguration config, ILogger<NotificationCleanupWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromHours(config.GetValue("Workers:NotificationCleanup:IntervalHours", 24));
        _retentionDays = config.GetValue("Workers:NotificationCleanup:RetentionDays", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationCleanupWorker started. Retention: {Days} days", _retentionDays);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();
                var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);
                var deleted = await db.Notifications
                    .Where(n => n.IsRead && n.Timestamp < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);
                if (deleted > 0) _logger.LogInformation("Deleted {Count} old read notifications", deleted);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error cleaning up notifications"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}

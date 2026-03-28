using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Background worker that polls for ML historical order sync requests and executes them.
/// Sync requests are enqueued via POST /api/integrations/mercadolivre/sync-orders.
/// </summary>
public class OrderSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OrderSyncWorker> _logger;
    private readonly TimeSpan _pollInterval;

    private const string SyncQueuePrefix = "ordersync:ml:queue:";

    public OrderSyncWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<OrderSyncWorker> logger)
    {
        _services = services;
        _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(config.GetValue("Workers:OrderSync:PollIntervalSeconds", 10));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderSyncWorker started. Poll interval: {Interval}", _pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingSyncsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OrderSyncWorker poll cycle");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingSyncsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        var connections = await db.MarketplaceConnections
            .IgnoreQueryFilters()
            .Where(c => c.MarketplaceId == "mercadolivre" && c.IsConnected)
            .Select(c => c.TenantId)
            .Distinct()
            .ToListAsync(ct);

        var cache = scope.ServiceProvider.GetRequiredService<Core.Interfaces.ICacheService>();

        foreach (var tenantId in connections)
        {
            var queued = await cache.GetAsync<OrderSyncRequest>($"{SyncQueuePrefix}{tenantId}", ct);
            if (queued is null) continue;

            _logger.LogInformation("Processing ML order sync for tenant {TenantId}", tenantId);

            try
            {
                using var syncScope = _services.CreateScope();
                var syncService = syncScope.ServiceProvider.GetRequiredService<IOrderSyncService>();
                await syncService.ExecuteSyncAsync(tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ML order sync failed for tenant {TenantId}", tenantId);
            }
        }
    }
}

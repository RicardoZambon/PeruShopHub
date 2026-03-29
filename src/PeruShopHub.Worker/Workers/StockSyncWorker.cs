using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Background worker that polls for pending ML stock sync requests and pushes
/// allocated quantities to Mercado Livre. Enqueued by InventoryService after
/// stock adjustments, reconciliation, and allocation changes.
/// </summary>
public class StockSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<StockSyncWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public StockSyncWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<StockSyncWorker> logger)
    {
        _services = services;
        _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(config.GetValue("Workers:StockSync:PollIntervalSeconds", 10));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StockSyncWorker started. Poll interval: {Interval}", _pollInterval);

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
                _logger.LogError(ex, "Error in StockSyncWorker poll cycle");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingSyncsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        // Get all tenants with active ML connections
        var tenantIds = await db.MarketplaceConnections
            .IgnoreQueryFilters()
            .Where(c => c.MarketplaceId == "mercadolivre" && c.IsConnected && c.Status == "Active")
            .Select(c => c.TenantId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                using var syncScope = _services.CreateScope();
                var syncService = syncScope.ServiceProvider.GetRequiredService<IStockSyncService>();
                await syncService.ExecutePendingSyncsAsync(tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ML stock sync failed for tenant {TenantId}", tenantId);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Background worker that polls for ML listing import requests and executes them.
/// Import requests are enqueued via POST /api/integrations/mercadolivre/import.
/// </summary>
public class MlListingImportWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MlListingImportWorker> _logger;
    private readonly TimeSpan _pollInterval;

    private const string ImportQueuePrefix = "import:ml:queue:";

    public MlListingImportWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<MlListingImportWorker> logger)
    {
        _services = services;
        _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(config.GetValue("Workers:MlListingImport:PollIntervalSeconds", 10));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MlListingImportWorker started. Poll interval: {Interval}", _pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingImportsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MlListingImportWorker poll cycle");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingImportsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        // Find all active ML connections to check for queued imports
        var connections = await db.MarketplaceConnections
            .IgnoreQueryFilters()
            .Where(c => c.MarketplaceId == "mercadolivre" && c.IsConnected)
            .Select(c => c.TenantId)
            .Distinct()
            .ToListAsync(ct);

        var cache = scope.ServiceProvider.GetRequiredService<Core.Interfaces.ICacheService>();

        foreach (var tenantId in connections)
        {
            var queued = await cache.GetAsync<string>($"{ImportQueuePrefix}{tenantId}", ct);
            if (queued is null) continue;

            _logger.LogInformation("Processing ML listing import for tenant {TenantId}", tenantId);

            try
            {
                // Create a new scope for the import to get fresh DbContext
                using var importScope = _services.CreateScope();
                var importService = importScope.ServiceProvider.GetRequiredService<IMlListingImportService>();
                await importService.ExecuteImportAsync(tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ML listing import failed for tenant {TenantId}", tenantId);
            }
        }
    }
}

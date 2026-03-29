using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Background worker that syncs ML claims/returns every 10 minutes.
/// Iterates all active ML connections and syncs open + recent claims.
/// </summary>
public class ClaimSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ClaimSyncWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public ClaimSyncWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<ClaimSyncWorker> logger)
    {
        _services = services;
        _logger = logger;
        _pollInterval = TimeSpan.FromMinutes(config.GetValue("Workers:ClaimSync:IntervalMinutes", 10));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ClaimSyncWorker started. Interval: {Interval}", _pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllTenantsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClaimSyncWorker");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task SyncAllTenantsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        var tenantIds = await db.MarketplaceConnections
            .IgnoreQueryFilters()
            .Where(c => c.MarketplaceId == "mercadolivre" && c.IsConnected && c.Status == "Active")
            .Select(c => c.TenantId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var syncScope = _services.CreateScope();
                var claimService = syncScope.ServiceProvider.GetRequiredService<IClaimService>();
                await claimService.SyncClaimsAsync(tenantId, ct);

                _logger.LogDebug("Synced claims for tenant {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync claims for tenant {TenantId}", tenantId);
            }
        }
    }
}

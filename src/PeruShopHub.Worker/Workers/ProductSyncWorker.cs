using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Periodically syncs products from Mercado Livre for all active connections.
/// Runs every 2 hours by default (configurable via Workers:ProductSync:IntervalMinutes).
/// </summary>
public class ProductSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ProductSyncWorker> _logger;
    private readonly TimeSpan _interval;

    public ProductSyncWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<ProductSyncWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Workers:ProductSync:IntervalMinutes", 120));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProductSyncWorker started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllConnectionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProductSyncWorker cycle");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task SyncAllConnectionsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        // Get all active ML connections across all tenants
        var connections = await db.MarketplaceConnections
            .IgnoreQueryFilters()
            .Where(c => c.MarketplaceId == "mercadolivre"
                     && c.IsConnected
                     && c.Status == "Active"
                     && c.ExternalUserId != null)
            .ToListAsync(ct);

        if (connections.Count == 0)
        {
            _logger.LogDebug("ProductSyncWorker: no active ML connections found");
            return;
        }

        _logger.LogInformation("ProductSyncWorker: syncing {Count} active connections", connections.Count);

        foreach (var connection in connections)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Each connection gets its own scope for isolation
                using var syncScope = _services.CreateScope();
                var syncService = syncScope.ServiceProvider.GetRequiredService<IProductSyncService>();
                var syncDb = syncScope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

                // Re-fetch connection in new scope (tracked by new DbContext)
                var conn = await syncDb.MarketplaceConnections
                    .IgnoreQueryFilters()
                    .FirstAsync(c => c.Id == connection.Id, ct);

                var result = await syncService.SyncConnectionAsync(connection.TenantId, conn, ct);

                _logger.LogInformation(
                    "ProductSync for tenant {TenantId}: checked={Checked}, updated={Updated}, created={Created}, errors={Errors}",
                    connection.TenantId, result.Checked, result.Updated, result.Created, result.Errors);

                // Notify on errors
                if (result.Errors > 0)
                {
                    syncDb.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        TenantId = connection.TenantId,
                        Type = "product_sync_errors",
                        Title = "Sincronização de produtos com erros",
                        Description = $"Sincronização concluída com {result.Errors} erro(s) em {result.Checked} itens verificados.",
                        Timestamp = DateTime.UtcNow,
                        NavigationTarget = "/produtos"
                    });
                    await syncDb.SaveChangesAsync(ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ProductSync failed for connection {ConnectionId} (Tenant: {TenantId})",
                    connection.Id, connection.TenantId);

                // Notify tenant about sync failure
                try
                {
                    using var errorScope = _services.CreateScope();
                    var errorDb = errorScope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();
                    errorDb.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        TenantId = connection.TenantId,
                        Type = "product_sync_failed",
                        Title = "Falha na sincronização de produtos",
                        Description = $"Erro ao sincronizar produtos com o Mercado Livre: {ex.Message}",
                        Timestamp = DateTime.UtcNow,
                        NavigationTarget = "/integracao/configuracoes"
                    });
                    await errorDb.SaveChangesAsync(ct);
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Failed to create error notification for tenant {TenantId}", connection.TenantId);
                }
            }
        }
    }
}

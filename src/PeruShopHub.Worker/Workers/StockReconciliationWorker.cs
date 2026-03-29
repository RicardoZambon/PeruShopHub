using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Periodic worker that compares local stock with ML stock for all tenants.
/// Runs every 6 hours (configurable). Auto-corrects small discrepancies,
/// flags large ones for manual review with notifications.
/// </summary>
public class StockReconciliationWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<StockReconciliationWorker> _logger;
    private readonly TimeSpan _interval;
    private readonly int _autoCorrectThreshold;

    public StockReconciliationWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<StockReconciliationWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromHours(config.GetValue("Workers:StockReconciliation:IntervalHours", 6));
        _autoCorrectThreshold = config.GetValue("Workers:StockReconciliation:AutoCorrectThreshold", 2);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StockReconciliationWorker started. Interval: {Interval}, AutoCorrectThreshold: {Threshold}",
            _interval, _autoCorrectThreshold);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunForAllTenantsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StockReconciliationWorker cycle");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunForAllTenantsAsync(CancellationToken ct)
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

        _logger.LogInformation("StockReconciliationWorker: found {Count} tenants with active ML connections", tenantIds.Count);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                using var tenantScope = _services.CreateScope();
                var reconciliationService = tenantScope.ServiceProvider.GetRequiredService<IStockReconciliationService>();
                var notificationDispatcher = tenantScope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

                var reportId = await reconciliationService.RunReconciliationAsync(tenantId, _autoCorrectThreshold, ct);

                // Check if report has manual review items and notify
                var reportDb = tenantScope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();
                var report = await reportDb.StockReconciliationReports
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.Id == reportId, ct);

                if (report is not null && report.ManualReviewRequired > 0)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Type = "stock_reconciliation",
                        Title = "Reconciliação de Estoque",
                        Description = $"Reconciliação concluída: {report.ManualReviewRequired} item(ns) requerem revisão manual. " +
                            $"{report.AutoCorrected} auto-corrigido(s), {report.Matches} ok.",
                        NavigationTarget = $"/estoque?tab=reconciliacao-ml&reportId={reportId}",
                        Timestamp = DateTime.UtcNow,
                    };
                    await notificationDispatcher.DispatchAsync(notification, ct);
                }

                _logger.LogInformation(
                    "Stock reconciliation completed for tenant {TenantId}: report {ReportId}, " +
                    "{ManualReview} manual review, {AutoCorrected} auto-corrected",
                    tenantId, reportId, report?.ManualReviewRequired ?? 0, report?.AutoCorrected ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock reconciliation failed for tenant {TenantId}", tenantId);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

/// <summary>
/// Background worker that reconciles orders missing real billing data from the ML Billing API.
/// Retry schedule: initial attempt at order creation, then 1h, 6h, 24h.
/// Only processes orders up to 7 days old.
/// </summary>
public class BillingReconciliationWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BillingReconciliationWorker> _logger;
    private readonly TimeSpan _interval;
    private const int MaxRetries = 4; // creation + 1h + 6h + 24h

    // Retry delays: after creation (0), 1h, 6h, 24h
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6),
        TimeSpan.FromHours(24),
        TimeSpan.FromHours(168) // 7 days = max age, stop retrying
    ];

    public BillingReconciliationWorker(IServiceProvider services, IConfiguration config, ILogger<BillingReconciliationWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Workers:BillingReconciliation:IntervalMinutes", 30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BillingReconciliationWorker started. Interval: {Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ReconcileBillingAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Error in billing reconciliation"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ReconcileBillingAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMlOrderMapper>();

        var cutoff = DateTime.UtcNow.AddDays(-7);

        // Find orders without billing data that are eligible for retry
        var orders = await db.Orders
            .IgnoreQueryFilters()
            .Where(o => o.BillingFetchedAt == null
                     && o.BillingRetryCount < MaxRetries
                     && o.CreatedAt >= cutoff
                     && !string.IsNullOrEmpty(o.ExternalOrderId))
            .ToListAsync(ct);

        if (orders.Count == 0) return;

        // Filter by retry schedule
        var now = DateTime.UtcNow;
        var eligible = orders.Where(o => IsEligibleForRetry(o, now)).ToList();

        if (eligible.Count == 0) return;

        _logger.LogInformation("Billing reconciliation: {Count} orders eligible for retry", eligible.Count);

        // Group by tenant to reuse adapter connection
        var byTenant = eligible.GroupBy(o => o.TenantId);

        foreach (var group in byTenant)
        {
            var tenantId = group.Key;

            // Find marketplace connection for this tenant
            var connection = await db.MarketplaceConnections
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId
                                       && c.MarketplaceId == "mercadolivre"
                                       && c.Status == "Active", ct);

            if (connection is null)
            {
                _logger.LogDebug("No active ML connection for tenant {TenantId}, skipping", tenantId);
                continue;
            }

            var adapter = scope.ServiceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre");
            if (adapter is null) continue;

            foreach (var order in group)
            {
                try
                {
                    await FetchAndMergeBillingAsync(db, adapter, mapper, order, tenantId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Billing fetch failed for order {OrderId} (retry {Retry})",
                        order.ExternalOrderId, order.BillingRetryCount);
                    order.BillingRetryCount++;
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task FetchAndMergeBillingAsync(
        PeruShopHubDbContext db, IMarketplaceAdapter adapter, IMlOrderMapper mapper,
        Order order, Guid tenantId, CancellationToken ct)
    {
        var fees = await adapter.GetOrderFeesAsync(order.ExternalOrderId, ct);

        if (fees.Count == 0)
        {
            order.BillingRetryCount++;
            _logger.LogDebug("No billing data for order {OrderId}, retry {Retry}",
                order.ExternalOrderId, order.BillingRetryCount);
            return;
        }

        // Merge API costs (same logic as webhook processing)
        foreach (var fee in fees)
        {
            var category = mapper.MapFeeTypeToCategory(fee.Type);

            var existing = await db.OrderCosts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.OrderId == order.Id && c.Category == category && c.Source == "API", ct);

            if (existing is not null)
            {
                existing.Value = Math.Abs(fee.Amount);
            }
            else
            {
                var calculated = await db.OrderCosts
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.OrderId == order.Id && c.Category == category && c.Source == "Calculated", ct);

                if (calculated is not null)
                {
                    calculated.Value = Math.Abs(fee.Amount);
                    calculated.Source = "API";
                    calculated.Description = $"ML API: {fee.Type}";
                }
                else
                {
                    db.OrderCosts.Add(new OrderCost
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        OrderId = order.Id,
                        Category = category,
                        Description = $"ML API: {fee.Type}",
                        Value = Math.Abs(fee.Amount),
                        Source = "API"
                    });
                }
            }
        }

        // Recalculate profit
        var totalCosts = await db.OrderCosts
            .IgnoreQueryFilters()
            .Where(c => c.OrderId == order.Id)
            .SumAsync(c => c.Value, ct);
        order.Profit = order.TotalAmount - totalCosts;

        order.BillingFetchedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Billing reconciled for order {OrderId}: {FeeCount} fees, profit updated to {Profit}",
            order.ExternalOrderId, fees.Count, order.Profit);
    }

    private static bool IsEligibleForRetry(Order order, DateTime now)
    {
        if (order.BillingRetryCount >= MaxRetries) return false;

        var retryIndex = order.BillingRetryCount;
        if (retryIndex >= RetryDelays.Length) return false;

        var nextRetryAfter = order.CreatedAt + RetryDelays[retryIndex];
        return now >= nextRetryAfter;
    }
}

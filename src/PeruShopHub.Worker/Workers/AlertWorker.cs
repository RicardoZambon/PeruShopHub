using Microsoft.EntityFrameworkCore;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Worker.Workers;

public class AlertWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AlertWorker> _logger;
    private readonly TimeSpan _interval;

    public AlertWorker(IServiceProvider services, IConfiguration config, ILogger<AlertWorker> logger)
    {
        _services = services;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Workers:Alert:IntervalMinutes", 60));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertWorker started. Interval: {Interval}", _interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await EvaluateAlertRules(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Error evaluating alert rules"); }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task EvaluateAlertRules(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeruShopHubDbContext>();

        // Use IgnoreQueryFilters since worker has no tenant context
        var rules = await db.AlertRules
            .IgnoreQueryFilters()
            .Where(r => r.IsActive)
            .ToListAsync(ct);

        if (rules.Count == 0) return;

        _logger.LogInformation("Evaluating {Count} active alert rules", rules.Count);

        foreach (var rule in rules)
        {
            try
            {
                switch (rule.Type)
                {
                    case "MarginBelow":
                        await CheckMarginBelow(db, rule, ct);
                        break;
                    case "CostIncrease":
                        await CheckCostIncrease(db, rule, ct);
                        break;
                    case "StockLow":
                        await CheckStockLow(db, rule, ct);
                        break;
                    default:
                        _logger.LogWarning("Unknown alert rule type: {Type}", rule.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating alert rule {RuleId} ({Type})", rule.Id, rule.Type);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task CheckMarginBelow(PeruShopHubDbContext db, AlertRule rule, CancellationToken ct)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // Get orders from last 30 days for the tenant
        var ordersQuery = db.Orders
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == rule.TenantId && o.OrderDate >= thirtyDaysAgo);

        if (rule.ProductId.HasValue)
        {
            ordersQuery = ordersQuery.Where(o =>
                o.Items.Any(i => i.ProductId == rule.ProductId.Value));
        }

        var orders = await ordersQuery
            .Select(o => new { o.Id, o.TotalAmount, CostSum = o.Costs.Sum(c => c.Value) })
            .ToListAsync(ct);

        if (orders.Count == 0) return;

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalCosts = orders.Sum(o => o.CostSum);
        var avgMargin = totalRevenue > 0 ? ((totalRevenue - totalCosts) / totalRevenue) * 100 : 0;

        if (avgMargin < rule.Threshold)
        {
            var productName = rule.ProductId.HasValue
                ? await db.Products.IgnoreQueryFilters().Where(p => p.Id == rule.ProductId).Select(p => p.Name).FirstOrDefaultAsync(ct)
                : null;

            var title = productName != null
                ? $"Margem baixa: {productName}"
                : "Margem média baixa (todos os produtos)";

            var description = $"Margem média dos últimos 30 dias: {avgMargin:N1}% (limite: {rule.Threshold:N1}%)";

            // Check if a similar unread notification already exists
            var exists = await db.Notifications
                .IgnoreQueryFilters()
                .AnyAsync(n => n.TenantId == rule.TenantId && n.Type == "margin_alert" && n.Title == title && !n.IsRead, ct);

            if (!exists)
            {
                db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    TenantId = rule.TenantId,
                    Type = "margin_alert",
                    Title = title,
                    Description = description,
                    Timestamp = DateTime.UtcNow,
                    NavigationTarget = rule.ProductId.HasValue ? $"/produtos/{rule.ProductId}" : "/dashboard"
                });

                _logger.LogInformation("Margin alert triggered for tenant {TenantId}: {Margin}% < {Threshold}%",
                    rule.TenantId, avgMargin, rule.Threshold);
            }
        }
    }

    private async Task CheckCostIncrease(PeruShopHubDbContext db, AlertRule rule, CancellationToken ct)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // Compare current weighted average cost vs 30 days ago using cost history
        var productsQuery = rule.ProductId.HasValue
            ? db.Products.IgnoreQueryFilters().Where(p => p.Id == rule.ProductId && p.TenantId == rule.TenantId)
            : db.Products.IgnoreQueryFilters().Where(p => p.TenantId == rule.TenantId && p.IsActive);

        var products = await productsQuery
            .Include(p => p.Variants)
            .ToListAsync(ct);

        foreach (var product in products)
        {
            var currentCost = product.Variants
                .Where(v => v.PurchaseCost.HasValue && v.PurchaseCost > 0)
                .Select(v => v.PurchaseCost!.Value)
                .DefaultIfEmpty(0).Average();
            if (currentCost == 0) continue;

            // Check cost history for 30 days ago
            var oldCostEntry = await db.ProductCostHistories
                .IgnoreQueryFilters()
                .Where(h => h.ProductId == product.Id && h.CreatedAt <= thirtyDaysAgo)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (oldCostEntry == null || oldCostEntry.NewCost == 0) continue;

            var increasePercent = ((currentCost - oldCostEntry.NewCost) / oldCostEntry.NewCost) * 100;

            if (increasePercent > rule.Threshold)
            {
                var title = $"Aumento de custo: {product.Name}";
                var description = $"Custo aumentou {increasePercent:N1}% nos últimos 30 dias (limite: {rule.Threshold:N1}%)";

                var exists = await db.Notifications
                    .IgnoreQueryFilters()
                    .AnyAsync(n => n.TenantId == rule.TenantId && n.Type == "cost_alert" && n.Title == title && !n.IsRead, ct);

                if (!exists)
                {
                    db.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        TenantId = rule.TenantId,
                        Type = "cost_alert",
                        Title = title,
                        Description = description,
                        Timestamp = DateTime.UtcNow,
                        NavigationTarget = $"/produtos/{product.Id}"
                    });

                    _logger.LogInformation("Cost increase alert for product {ProductId}: {Increase}% > {Threshold}%",
                        product.Id, increasePercent, rule.Threshold);
                }
            }
        }
    }

    private async Task CheckStockLow(PeruShopHubDbContext db, AlertRule rule, CancellationToken ct)
    {
        var productsQuery = rule.ProductId.HasValue
            ? db.Products.IgnoreQueryFilters().Where(p => p.Id == rule.ProductId && p.TenantId == rule.TenantId)
            : db.Products.IgnoreQueryFilters().Where(p => p.TenantId == rule.TenantId && p.IsActive);

        var products = await productsQuery
            .Include(p => p.Variants)
            .ToListAsync(ct);

        foreach (var product in products)
        {
            var totalStock = product.Variants.Sum(v => v.Stock);

            if (totalStock <= rule.Threshold)
            {
                var title = $"Estoque baixo: {product.Name}";
                var description = $"{product.Name} tem {totalStock} unidades (limite: {rule.Threshold:N0})";

                var exists = await db.Notifications
                    .IgnoreQueryFilters()
                    .AnyAsync(n => n.TenantId == rule.TenantId && n.Type == "stock_low_alert" && n.Title == title && !n.IsRead, ct);

                if (!exists)
                {
                    db.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        TenantId = rule.TenantId,
                        Type = "stock_low_alert",
                        Title = title,
                        Description = description,
                        Timestamp = DateTime.UtcNow,
                        NavigationTarget = $"/produtos/{product.Id}"
                    });

                    _logger.LogInformation("Stock low alert for product {ProductId}: {Stock} <= {Threshold}",
                        product.Id, totalStock, rule.Threshold);
                }
            }
        }
    }
}

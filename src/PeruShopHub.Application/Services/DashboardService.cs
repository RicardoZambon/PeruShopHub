using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Dashboard;
using PeruShopHub.Application.DTOs.Finance;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICacheService _cache;

    public DashboardService(PeruShopHubDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(string period, CancellationToken ct = default)
    {
        var cacheKey = $"dashboard:summary:{period}";
        var cached = await _cache.GetAsync<DashboardSummaryDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var (start, end) = ParsePeriod(period);
        var periodLength = end - start;
        var prevStart = start - periodLength;
        var prevEnd = start;

        var currentOrders = await _db.Orders
            .Where(o => o.OrderDate >= start && o.OrderDate < end)
            .ToListAsync(ct);

        var prevOrders = await _db.Orders
            .Where(o => o.OrderDate >= prevStart && o.OrderDate < prevEnd)
            .ToListAsync(ct);

        var salesCount = currentOrders.Count;
        var prevSalesCount = prevOrders.Count;

        var grossRevenue = currentOrders.Sum(o => o.TotalAmount);
        var prevGrossRevenue = prevOrders.Sum(o => o.TotalAmount);

        var netProfit = currentOrders.Sum(o => o.Profit);
        var prevNetProfit = prevOrders.Sum(o => o.Profit);

        var avgMargin = grossRevenue != 0
            ? Math.Round(netProfit / grossRevenue * 100, 2)
            : 0m;
        var prevAvgMargin = prevGrossRevenue != 0
            ? Math.Round(prevNetProfit / prevGrossRevenue * 100, 2)
            : 0m;

        var kpis = new List<KpiCardDto>
        {
            new("Vendas",
                salesCount.ToString(),
                prevSalesCount.ToString(),
                CalculateChangePercent(salesCount, prevSalesCount),
                GetChangeDirection(salesCount, prevSalesCount),
                "shopping_cart"),
            new("Receita Bruta",
                grossRevenue.ToString("F2"),
                prevGrossRevenue.ToString("F2"),
                CalculateChangePercent(grossRevenue, prevGrossRevenue),
                GetChangeDirection(grossRevenue, prevGrossRevenue),
                "attach_money"),
            new("Lucro Liquido",
                netProfit.ToString("F2"),
                prevNetProfit.ToString("F2"),
                CalculateChangePercent(netProfit, prevNetProfit),
                GetChangeDirection(netProfit, prevNetProfit),
                "trending_up"),
            new("Margem Media",
                avgMargin.ToString("F2"),
                prevAvgMargin.ToString("F2"),
                avgMargin - prevAvgMargin,
                GetChangeDirection(avgMargin, prevAvgMargin),
                "percent")
        };

        var pendingActions = await BuildPendingActionsAsync(ct);

        var chartDays = Math.Min(7, (int)periodLength.TotalDays);
        var chartStart = end.AddDays(-chartDays);
        var revenueChart = await _db.Orders
            .Where(o => o.OrderDate >= chartStart && o.OrderDate < end)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new ChartDataPointDto(
                g.Key.ToString("dd/MM"),
                g.Sum(o => o.TotalAmount),
                g.Sum(o => o.Profit)))
            .OrderBy(c => c.Label)
            .ToListAsync(ct);

        var ordersChart = await _db.Orders
            .Where(o => o.OrderDate >= chartStart && o.OrderDate < end)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new ChartDataPointDto(
                g.Key.ToString("dd/MM"),
                g.Count(),
                null))
            .OrderBy(c => c.Label)
            .ToListAsync(ct);

        var topProducts = await GetProductRankingsAsync(start, end, 5, descending: true, ct);

        var result = new DashboardSummaryDto(kpis, topProducts, pendingActions, revenueChart, ordersChart);
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(60), ct);
        return result;
    }

    public async Task<IReadOnlyList<ChartDataPointDto>> GetRevenueProfitChartAsync(int days, CancellationToken ct = default)
    {
        var end = DateTime.UtcNow.Date.AddDays(1);
        var start = end.AddDays(-days);

        var data = await _db.Orders
            .Where(o => o.OrderDate >= start && o.OrderDate < end)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new ChartDataPointDto(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(o => o.TotalAmount),
                g.Sum(o => o.Profit)))
            .OrderBy(c => c.Label)
            .ToListAsync(ct);

        return data;
    }

    public async Task<IReadOnlyList<CostBreakdownDto>> GetCostBreakdownAsync(string period, CancellationToken ct = default)
    {
        var (start, end) = ParsePeriod(period);

        var orderIds = await _db.Orders
            .Where(o => o.OrderDate >= start && o.OrderDate < end)
            .Select(o => o.Id)
            .ToListAsync(ct);

        var costs = await _db.OrderCosts
            .Where(c => orderIds.Contains(c.OrderId))
            .GroupBy(c => c.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(c => c.Value) })
            .ToListAsync(ct);

        var grandTotal = costs.Sum(c => c.Total);

        var breakdown = costs
            .Select(c => new CostBreakdownDto(
                c.Category,
                c.Total,
                grandTotal != 0 ? Math.Round(c.Total / grandTotal * 100, 2) : 0))
            .OrderByDescending(c => c.Total)
            .ToList();

        return breakdown;
    }

    public async Task<IReadOnlyList<ProductRankingDto>> GetTopProductsAsync(int limit, CancellationToken ct = default)
    {
        var (start, end) = ParsePeriod("30dias");
        return await GetProductRankingsAsync(start, end, limit, descending: true, ct);
    }

    public async Task<IReadOnlyList<ProductRankingDto>> GetLeastProfitableAsync(int limit, CancellationToken ct = default)
    {
        var (start, end) = ParsePeriod("30dias");
        return await GetProductRankingsAsync(start, end, limit, descending: false, ct);
    }

    public async Task<IReadOnlyList<PendingActionDto>> GetPendingActionsAsync(CancellationToken ct = default)
    {
        return await BuildPendingActionsAsync(ct);
    }

    private async Task<List<PendingActionDto>> BuildPendingActionsAsync(CancellationToken ct)
    {
        var pendingActions = new List<PendingActionDto>();

        var questionCount = await _db.Notifications
            .Where(n => !n.IsRead && n.Type == "question")
            .CountAsync(ct);
        if (questionCount > 0)
            pendingActions.Add(new PendingActionDto("question", "Perguntas sem resposta",
                $"{questionCount} pergunta(s) aguardando resposta", "/questions", questionCount));

        var paidOrdersCount = await _db.Orders
            .Where(o => o.Status == "Pago")
            .CountAsync(ct);
        if (paidOrdersCount > 0)
            pendingActions.Add(new PendingActionDto("order", "Pedidos pagos",
                $"{paidOrdersCount} pedido(s) aguardando envio", "/orders", paidOrdersCount));

        var stockAlertCount = await _db.Notifications
            .Where(n => !n.IsRead && n.Type == "stock_alert")
            .CountAsync(ct);
        if (stockAlertCount > 0)
            pendingActions.Add(new PendingActionDto("stock_alert", "Alertas de estoque",
                $"{stockAlertCount} produto(s) com estoque baixo", "/products", stockAlertCount));

        return pendingActions;
    }

    private async Task<List<ProductRankingDto>> GetProductRankingsAsync(
        DateTime start, DateTime end, int limit, bool descending, CancellationToken ct)
    {
        var orderIds = await _db.Orders
            .Where(o => o.OrderDate >= start && o.OrderDate < end)
            .Select(o => o.Id)
            .ToListAsync(ct);

        var orderItems = await _db.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Order)
            .ToListAsync(ct);

        var grouped = orderItems
            .GroupBy(oi => new { oi.Sku, oi.Name, oi.ProductId })
            .Select(g =>
            {
                var revenue = g.Sum(oi => oi.Subtotal);
                var profit = g.Sum(oi =>
                {
                    var orderTotal = oi.Order.TotalAmount;
                    return orderTotal != 0
                        ? oi.Subtotal / orderTotal * oi.Order.Profit
                        : 0m;
                });
                var margin = revenue != 0 ? Math.Round(profit / revenue * 100, 2) : 0m;
                return new ProductRankingDto(
                    g.Key.ProductId ?? Guid.Empty,
                    g.Key.Name,
                    g.Key.Sku,
                    g.Sum(oi => oi.Quantity),
                    revenue,
                    Math.Round(profit, 4),
                    margin);
            });

        var sorted = descending
            ? grouped.OrderByDescending(p => p.Profit)
            : grouped.OrderBy(p => p.Profit);

        return sorted.Take(limit).ToList();
    }

    private static (DateTime Start, DateTime End) ParsePeriod(string period)
    {
        var end = DateTime.UtcNow.Date.AddDays(1);
        var start = period.ToLowerInvariant() switch
        {
            "hoje" => DateTime.UtcNow.Date,
            "7dias" => end.AddDays(-7),
            "30dias" => end.AddDays(-30),
            _ => end.AddDays(-30)
        };
        return (start, end);
    }

    private static decimal? CalculateChangePercent(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0 : 100;
        return Math.Round((current - previous) / Math.Abs(previous) * 100, 2);
    }

    private static string? GetChangeDirection(decimal current, decimal previous)
    {
        if (current > previous) return "up";
        if (current < previous) return "down";
        return "neutral";
    }
}

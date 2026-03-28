using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Dashboard;
using PeruShopHub.Application.DTOs.Finance;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class FinanceService : IFinanceService
{
    private readonly PeruShopHubDbContext _db;

    public FinanceService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<FinanceSummaryDto> GetSummaryAsync(string period, CancellationToken ct = default)
    {
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

        var totalRevenue = currentOrders.Sum(o => o.TotalAmount);
        var totalProfit = currentOrders.Sum(o => o.Profit);
        var totalCosts = totalRevenue - totalProfit;
        var avgMargin = totalRevenue != 0
            ? Math.Round(totalProfit / totalRevenue * 100, 2)
            : 0m;
        var avgTicket = currentOrders.Count > 0
            ? Math.Round(totalRevenue / currentOrders.Count, 2)
            : 0m;

        var prevRevenue = prevOrders.Sum(o => o.TotalAmount);
        var prevProfit = prevOrders.Sum(o => o.Profit);

        var revenueChange = prevRevenue != 0
            ? Math.Round((totalRevenue - prevRevenue) / Math.Abs(prevRevenue) * 100, 2)
            : (totalRevenue == 0 ? 0m : 100m);
        var profitChange = prevProfit != 0
            ? Math.Round((totalProfit - prevProfit) / Math.Abs(prevProfit) * 100, 2)
            : (totalProfit == 0 ? 0m : 100m);

        var orderIds = currentOrders.Select(o => o.Id).ToList();
        var costs = await _db.OrderCosts
            .Where(c => orderIds.Contains(c.OrderId))
            .GroupBy(c => c.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(c => c.Value) })
            .ToListAsync(ct);

        var costTotal = costs.Sum(c => c.Total);
        var breakdown = costs
            .OrderByDescending(c => c.Total)
            .Select(c => new CostBreakdownDto(
                c.Category,
                c.Total,
                costTotal != 0 ? Math.Round(c.Total / costTotal * 100, 2) : 0m))
            .ToList();

        return new FinanceSummaryDto(
            totalRevenue, totalCosts, totalProfit, avgMargin, avgTicket,
            revenueChange, profitChange, breakdown);
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

    public async Task<IReadOnlyList<MarginChartPointDto>> GetMarginChartAsync(int days, CancellationToken ct = default)
    {
        var end = DateTime.UtcNow.Date.AddDays(1);
        var start = end.AddDays(-days);

        var data = await _db.Orders
            .Where(o => o.OrderDate >= start && o.OrderDate < end)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount),
                Profit = g.Sum(o => o.Profit)
            })
            .OrderBy(g => g.Date)
            .ToListAsync(ct);

        var result = data
            .Select(d => new MarginChartPointDto(
                d.Date.ToString("yyyy-MM-dd"),
                d.Revenue != 0 ? Math.Round(d.Profit / d.Revenue * 100, 2) : 0m))
            .ToList();

        return result;
    }

    public async Task<PagedResult<SkuProfitabilityDetailDto>> GetSkuProfitabilityAsync(
        int page, int pageSize, string sortBy, string sortDir,
        string? search = null, decimal? minMargin = null, decimal? maxMargin = null,
        DateTime? dateFrom = null, DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        // When date filters are applied, compute from source tables instead of materialized view
        if (dateFrom.HasValue || dateTo.HasValue)
        {
            return await GetSkuProfitabilityFromSourceAsync(
                page, pageSize, sortBy, sortDir, search, minMargin, maxMargin,
                dateFrom, dateTo, ct);
        }

        // Use materialized view for non-date-filtered queries (fast path)
        var query = _db.SkuProfitabilityViews.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(v => v.Sku.ToLower().Contains(term) || v.Name.ToLower().Contains(term));
        }

        if (minMargin.HasValue)
            query = query.Where(v => v.AvgMargin >= minMargin.Value);

        if (maxMargin.HasValue)
            query = query.Where(v => v.AvgMargin <= maxMargin.Value);

        var ordered = ApplySkuSorting(query, sortBy, sortDir);

        var totalCount = await query.CountAsync(ct);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new SkuProfitabilityDetailDto(
                v.ProductId ?? Guid.Empty,
                v.Sku,
                v.Name,
                v.TotalUnits,
                Math.Round(v.TotalRevenue, 4),
                Math.Round(v.CostCmv, 4),
                Math.Round(v.CostCommissions, 4),
                Math.Round(v.CostShipping, 4),
                Math.Round(v.CostTaxes, 4),
                Math.Round(v.TotalCosts, 4),
                Math.Round(v.TotalProfit, 4),
                v.AvgMargin))
            .ToListAsync(ct);

        return new PagedResult<SkuProfitabilityDetailDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task RefreshSkuProfitabilityAsync(CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY sku_profitability;", ct);
    }

    private async Task<PagedResult<SkuProfitabilityDetailDto>> GetSkuProfitabilityFromSourceAsync(
        int page, int pageSize, string sortBy, string sortDir,
        string? search, decimal? minMargin, decimal? maxMargin,
        DateTime? dateFrom, DateTime? dateTo,
        CancellationToken ct)
    {
        var itemsQuery = _db.OrderItems.AsQueryable();

        if (dateFrom.HasValue)
            itemsQuery = itemsQuery.Where(oi => oi.Order.OrderDate >= dateFrom.Value);
        if (dateTo.HasValue)
            itemsQuery = itemsQuery.Where(oi => oi.Order.OrderDate < dateTo.Value);

        var itemsWithCosts = await itemsQuery
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Costs)
            .ToListAsync(ct);

        var skuGroups = itemsWithCosts
            .GroupBy(oi => new { oi.Sku, oi.Name, oi.ProductId })
            .Select(g =>
            {
                var revenue = g.Sum(oi => oi.Subtotal);
                var unitsSold = g.Sum(oi => oi.Quantity);

                decimal cmv = 0, commissions = 0, shipping = 0, taxes = 0, otherCosts = 0;
                foreach (var item in g)
                {
                    var orderTotal = item.Order.TotalAmount;
                    if (orderTotal == 0) continue;
                    var proportion = item.Subtotal / orderTotal;

                    foreach (var cost in item.Order.Costs)
                    {
                        var allocated = cost.Value * proportion;
                        switch (cost.Category.ToLowerInvariant())
                        {
                            case "product_cost":
                            case "cmv":
                                cmv += allocated;
                                break;
                            case "marketplace_commission":
                            case "commission":
                                commissions += allocated;
                                break;
                            case "shipping_seller":
                            case "shipping":
                                shipping += allocated;
                                break;
                            case "tax":
                            case "taxes":
                                taxes += allocated;
                                break;
                            default:
                                otherCosts += allocated;
                                break;
                        }
                    }
                }

                var totalCosts = cmv + commissions + shipping + taxes + otherCosts;
                var profit = revenue - totalCosts;
                var margin = revenue != 0 ? Math.Round(profit / revenue * 100, 2) : 0m;

                return new SkuProfitabilityDetailDto(
                    g.Key.ProductId ?? Guid.Empty,
                    g.Key.Sku,
                    g.Key.Name,
                    unitsSold,
                    Math.Round(revenue, 4),
                    Math.Round(cmv, 4),
                    Math.Round(commissions, 4),
                    Math.Round(shipping, 4),
                    Math.Round(taxes, 4),
                    Math.Round(totalCosts, 4),
                    Math.Round(profit, 4),
                    margin);
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            skuGroups = skuGroups
                .Where(s => s.Sku.ToLower().Contains(term) || s.Name.ToLower().Contains(term))
                .ToList();
        }

        if (minMargin.HasValue)
            skuGroups = skuGroups.Where(s => s.Margin >= minMargin.Value).ToList();

        if (maxMargin.HasValue)
            skuGroups = skuGroups.Where(s => s.Margin <= maxMargin.Value).ToList();

        var sorted = ApplySkuSortingInMemory(skuGroups, sortBy, sortDir);

        var totalCount = skuGroups.Count;
        var items = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<SkuProfitabilityDetailDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static IQueryable<SkuProfitabilityView> ApplySkuSorting(
        IQueryable<SkuProfitabilityView> query, string sortBy, string sortDir)
    {
        return (sortBy.ToLowerInvariant(), sortDir.ToLowerInvariant()) switch
        {
            ("margin", "asc") => query.OrderBy(s => s.AvgMargin),
            ("margin", _) => query.OrderByDescending(s => s.AvgMargin),
            ("profit", "asc") => query.OrderBy(s => s.TotalProfit),
            ("profit", _) => query.OrderByDescending(s => s.TotalProfit),
            ("revenue", "asc") => query.OrderBy(s => s.TotalRevenue),
            ("revenue", _) => query.OrderByDescending(s => s.TotalRevenue),
            ("unitssold", "asc") => query.OrderBy(s => s.TotalUnits),
            ("unitssold", _) => query.OrderByDescending(s => s.TotalUnits),
            ("totalcosts", "asc") => query.OrderBy(s => s.TotalCosts),
            ("totalcosts", _) => query.OrderByDescending(s => s.TotalCosts),
            ("sku", "asc") => query.OrderBy(s => s.Sku),
            ("sku", _) => query.OrderByDescending(s => s.Sku),
            ("name", "asc") => query.OrderBy(s => s.Name),
            ("name", _) => query.OrderByDescending(s => s.Name),
            _ => query.OrderByDescending(s => s.AvgMargin)
        };
    }

    private static IEnumerable<SkuProfitabilityDetailDto> ApplySkuSortingInMemory(
        IEnumerable<SkuProfitabilityDetailDto> items, string sortBy, string sortDir)
    {
        return (sortBy.ToLowerInvariant(), sortDir.ToLowerInvariant()) switch
        {
            ("margin", "asc") => items.OrderBy(s => s.Margin),
            ("margin", _) => items.OrderByDescending(s => s.Margin),
            ("profit", "asc") => items.OrderBy(s => s.Profit),
            ("profit", _) => items.OrderByDescending(s => s.Profit),
            ("revenue", "asc") => items.OrderBy(s => s.Revenue),
            ("revenue", _) => items.OrderByDescending(s => s.Revenue),
            ("unitssold", "asc") => items.OrderBy(s => s.UnitsSold),
            ("unitssold", _) => items.OrderByDescending(s => s.UnitsSold),
            ("totalcosts", "asc") => items.OrderBy(s => s.TotalCosts),
            ("totalcosts", _) => items.OrderByDescending(s => s.TotalCosts),
            ("sku", "asc") => items.OrderBy(s => s.Sku),
            ("sku", _) => items.OrderByDescending(s => s.Sku),
            ("name", "asc") => items.OrderBy(s => s.Name),
            ("name", _) => items.OrderByDescending(s => s.Name),
            _ => items.OrderByDescending(s => s.Margin)
        };
    }

    public async Task<IReadOnlyList<MonthlyReconciliationDto>> GetReconciliationAsync(int year, CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .Where(o => o.OrderDate.Year == year)
            .ToListAsync(ct);

        var monthNames = new[]
        {
            "Janeiro", "Fevereiro", "Marco", "Abril", "Maio", "Junho",
            "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
        };

        var result = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var monthOrders = orders.Where(o => o.OrderDate.Month == month).ToList();
                var expected = monthOrders.Sum(o => o.TotalAmount);
                var deposited = monthOrders.Sum(o => o.PaymentAmount ?? o.TotalAmount);
                return new MonthlyReconciliationDto(
                    month,
                    monthNames[month - 1],
                    expected,
                    deposited,
                    deposited - expected);
            })
            .ToList();

        return result;
    }

    public async Task<IReadOnlyList<AbcProductDto>> GetAbcCurveAsync(
        DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default)
    {
        List<(Guid ProductId, string Sku, string Name, decimal Revenue, decimal Profit)> products;

        if (dateFrom is null && dateTo is null)
        {
            // Fast path: use materialized view
            var viewData = await _db.SkuProfitabilityViews
                .Select(v => new { v.ProductId, v.Sku, v.Name, v.TotalRevenue, v.TotalProfit })
                .ToListAsync(ct);
            products = viewData
                .Select(v => (v.ProductId ?? Guid.Empty, v.Sku, v.Name, v.TotalRevenue, v.TotalProfit))
                .ToList();
        }
        else
        {
            // Date-filtered: query source tables
            var query = _db.OrderItems.Include(oi => oi.Order).AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(oi => oi.Order.OrderDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(oi => oi.Order.OrderDate < dateTo.Value.AddDays(1));

            var items = await query.ToListAsync(ct);

            products = items
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
                    return (
                        ProductId: g.Key.ProductId ?? Guid.Empty,
                        Sku: g.Key.Sku,
                        Name: g.Key.Name,
                        Revenue: revenue,
                        Profit: profit
                    );
                })
                .ToList();
        }

        // Sort by revenue descending for ABC classification
        products = products.OrderByDescending(p => p.Revenue).ToList();

        var totalRevenue = products.Sum(p => p.Revenue);
        var cumulative = 0m;
        var result = new List<AbcProductDto>();

        foreach (var p in products)
        {
            cumulative += p.Revenue;
            var cumulativePct = totalRevenue != 0
                ? Math.Round(cumulative / totalRevenue * 100, 2)
                : 0m;

            var classification = cumulativePct <= 80 ? "A"
                : cumulativePct <= 95 ? "B"
                : "C";

            var margin = p.Revenue != 0
                ? Math.Round(p.Profit / p.Revenue * 100, 2)
                : 0m;

            result.Add(new AbcProductDto(
                p.ProductId,
                p.Sku,
                p.Name,
                Math.Round(p.Revenue, 4),
                Math.Round(p.Profit, 4),
                margin,
                cumulativePct,
                classification));
        }

        return result;
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
}

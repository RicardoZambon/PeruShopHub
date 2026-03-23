using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Dashboard;
using PeruShopHub.Application.DTOs.Finance;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/finance")]
public class FinanceController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    public FinanceController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<FinanceSummaryDto>> GetSummary(
        [FromQuery] string period = "30dias",
        CancellationToken ct = default)
    {
        var (start, end) = ParsePeriod(period);
        var periodLength = end - start;
        var prevStart = start - periodLength;
        var prevEnd = start;

        // Current period
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

        // Cost breakdown
        var orderIds = currentOrders.Select(o => o.Id).ToList();
        var costs = await _db.OrderCosts
            .Where(c => orderIds.Contains(c.OrderId))
            .GroupBy(c => c.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(c => c.Value) })
            .ToListAsync(ct);

        var costTotal = costs.Sum(c => c.Total);
        var breakdown = costs
            .Select(c => new CostBreakdownDto(
                c.Category,
                c.Total,
                costTotal != 0 ? Math.Round(c.Total / costTotal * 100, 2) : 0m))
            .OrderByDescending(c => c.Total)
            .ToList();

        return Ok(new FinanceSummaryDto(
            totalRevenue, totalCosts, totalProfit, avgMargin, avgTicket,
            revenueChange, profitChange, breakdown));
    }

    [HttpGet("chart/revenue-profit")]
    public async Task<ActionResult<IReadOnlyList<ChartDataPointDto>>> GetRevenueProfit(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
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

        return Ok(data);
    }

    [HttpGet("chart/margin")]
    public async Task<ActionResult<IReadOnlyList<MarginChartPointDto>>> GetMarginChart(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
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

        return Ok(result);
    }

    [HttpGet("sku-profitability")]
    public async Task<ActionResult<PagedResult<SkuProfitabilityDetailDto>>> GetSkuProfitability(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "margin",
        [FromQuery] string sortDir = "desc",
        CancellationToken ct = default)
    {
        // Get all order items with their orders and costs
        var itemsWithCosts = await _db.OrderItems
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Costs)
            .ToListAsync(ct);

        // Group by SKU
        var skuGroups = itemsWithCosts
            .GroupBy(oi => new { oi.Sku, oi.Name, oi.ProductId })
            .Select(g =>
            {
                var revenue = g.Sum(oi => oi.Subtotal);
                var unitsSold = g.Sum(oi => oi.Quantity);

                // Distribute costs proportionally by item subtotal within each order
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

        // Sort
        var sorted = (sortBy.ToLowerInvariant(), sortDir.ToLowerInvariant()) switch
        {
            ("margin", "asc") => skuGroups.OrderBy(s => s.Margin),
            ("margin", _) => skuGroups.OrderByDescending(s => s.Margin),
            ("profit", "asc") => skuGroups.OrderBy(s => s.Profit),
            ("profit", _) => skuGroups.OrderByDescending(s => s.Profit),
            ("revenue", "asc") => skuGroups.OrderBy(s => s.Revenue),
            ("revenue", _) => skuGroups.OrderByDescending(s => s.Revenue),
            ("unitssold", "asc") => skuGroups.OrderBy(s => s.UnitsSold),
            ("unitssold", _) => skuGroups.OrderByDescending(s => s.UnitsSold),
            ("sku", "asc") => skuGroups.OrderBy(s => s.Sku),
            ("sku", _) => skuGroups.OrderByDescending(s => s.Sku),
            _ => skuGroups.OrderByDescending(s => s.Margin)
        };

        var totalCount = skuGroups.Count;
        var items = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PagedResult<SkuProfitabilityDetailDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("reconciliation")]
    public async Task<ActionResult<IReadOnlyList<MonthlyReconciliationDto>>> GetReconciliation(
        [FromQuery] int year = 2026,
        CancellationToken ct = default)
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
                // Fabricate deposited as expected minus costs (using profit as proxy for deposited)
                var deposited = monthOrders.Sum(o => o.PaymentAmount ?? o.TotalAmount);
                return new MonthlyReconciliationDto(
                    month,
                    monthNames[month - 1],
                    expected,
                    deposited,
                    deposited - expected);
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("abc-curve")]
    public async Task<ActionResult<IReadOnlyList<AbcProductDto>>> GetAbcCurve(
        CancellationToken ct = default)
    {
        var itemsWithOrders = await _db.OrderItems
            .Include(oi => oi.Order)
            .ToListAsync(ct);

        var products = itemsWithOrders
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
                return new
                {
                    g.Key.ProductId,
                    g.Key.Sku,
                    g.Key.Name,
                    Revenue = revenue,
                    Profit = profit
                };
            })
            .OrderByDescending(p => p.Profit)
            .ToList();

        var totalProfit = products.Sum(p => p.Profit);
        var cumulative = 0m;
        var result = new List<AbcProductDto>();

        foreach (var p in products)
        {
            cumulative += p.Profit;
            var cumulativePct = totalProfit != 0
                ? Math.Round(cumulative / totalProfit * 100, 2)
                : 0m;

            var classification = cumulativePct <= 80 ? "A"
                : cumulativePct <= 95 ? "B"
                : "C";

            result.Add(new AbcProductDto(
                p.ProductId ?? Guid.Empty,
                p.Sku,
                p.Name,
                Math.Round(p.Revenue, 4),
                Math.Round(p.Profit, 4),
                cumulativePct,
                classification));
        }

        return Ok(result);
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

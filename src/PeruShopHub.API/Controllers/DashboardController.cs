using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Dashboard;
using PeruShopHub.Application.DTOs.Finance;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] string period = "30dias",
        CancellationToken ct = default)
    {
        var result = await _dashboardService.GetSummaryAsync(period, ct);
        return Ok(result);
    }

    [HttpGet("chart/revenue-profit")]
    public async Task<ActionResult<IReadOnlyList<ChartDataPointDto>>> GetRevenueProfit(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var data = await _dashboardService.GetRevenueProfitChartAsync(days, ct);
        return Ok(data);
    }

    [HttpGet("chart/cost-breakdown")]
    public async Task<ActionResult<IReadOnlyList<CostBreakdownDto>>> GetCostBreakdown(
        [FromQuery] string period = "30dias",
        CancellationToken ct = default)
    {
        var data = await _dashboardService.GetCostBreakdownAsync(period, ct);
        return Ok(data);
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<IReadOnlyList<ProductRankingDto>>> GetTopProducts(
        [FromQuery] int limit = 5,
        CancellationToken ct = default)
    {
        var products = await _dashboardService.GetTopProductsAsync(limit, ct);
        return Ok(products);
    }

    [HttpGet("least-profitable")]
    public async Task<ActionResult<IReadOnlyList<ProductRankingDto>>> GetLeastProfitable(
        [FromQuery] int limit = 5,
        CancellationToken ct = default)
    {
        var products = await _dashboardService.GetLeastProfitableAsync(limit, ct);
        return Ok(products);
    }

    [HttpGet("pending-actions")]
    public async Task<ActionResult<IReadOnlyList<PendingActionDto>>> GetPendingActions(
        CancellationToken ct = default)
    {
        var actions = await _dashboardService.GetPendingActionsAsync(ct);
        return Ok(actions);
    }
}

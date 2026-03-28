using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Dashboard;
using PeruShopHub.Application.DTOs.Finance;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/finance")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<FinanceSummaryDto>> GetSummary(
        [FromQuery] string period = "30dias",
        CancellationToken ct = default)
    {
        var result = await _financeService.GetSummaryAsync(period, ct);
        return Ok(result);
    }

    [HttpGet("chart/revenue-profit")]
    public async Task<ActionResult<IReadOnlyList<ChartDataPointDto>>> GetRevenueProfit(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var data = await _financeService.GetRevenueProfitChartAsync(days, ct);
        return Ok(data);
    }

    [HttpGet("chart/margin")]
    public async Task<ActionResult<IReadOnlyList<MarginChartPointDto>>> GetMarginChart(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var data = await _financeService.GetMarginChartAsync(days, ct);
        return Ok(data);
    }

    [HttpGet("sku-profitability")]
    public async Task<ActionResult<PagedResult<SkuProfitabilityDetailDto>>> GetSkuProfitability(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "margin",
        [FromQuery] string sortDir = "desc",
        [FromQuery] string? search = null,
        [FromQuery] decimal? minMargin = null,
        [FromQuery] decimal? maxMargin = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _financeService.GetSkuProfitabilityAsync(
            page, pageSize, sortBy, sortDir, search, minMargin, maxMargin, dateFrom, dateTo, ct);
        return Ok(result);
    }

    [HttpPost("sku-profitability/refresh")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RefreshSkuProfitability(CancellationToken ct = default)
    {
        await _financeService.RefreshSkuProfitabilityAsync(ct);
        return Ok(new { message = "Materialized view refreshed successfully" });
    }

    [HttpGet("reconciliation")]
    public async Task<ActionResult<IReadOnlyList<MonthlyReconciliationDto>>> GetReconciliation(
        [FromQuery] int year = 2026,
        CancellationToken ct = default)
    {
        var data = await _financeService.GetReconciliationAsync(year, ct);
        return Ok(data);
    }

    [HttpGet("abc-curve")]
    public async Task<ActionResult<IReadOnlyList<AbcProductDto>>> GetAbcCurve(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var data = await _financeService.GetAbcCurveAsync(dateFrom, dateTo, ct);
        return Ok(data);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("profitability/pdf")]
    public async Task<IActionResult> ProfitabilityPdf(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var bytes = await _reportService.GenerateProfitabilityReportAsync(dateFrom, dateTo, ct);
        var fileName = $"lucratividade_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("orders/pdf")]
    public async Task<IActionResult> OrdersPdf(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var bytes = await _reportService.GenerateOrderReportAsync(dateFrom, dateTo, ct);
        var fileName = $"vendas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("inventory/pdf")]
    public async Task<IActionResult> InventoryPdf(CancellationToken ct = default)
    {
        var bytes = await _reportService.GenerateInventoryReportAsync(ct);
        var fileName = $"estoque_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        return File(bytes, "application/pdf", fileName);
    }
}

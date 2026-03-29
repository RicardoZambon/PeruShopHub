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

    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("profitability/excel")]
    public async Task<IActionResult> ProfitabilityExcel(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var bytes = await _reportService.ExportProfitabilityToExcelAsync(dateFrom, dateTo, ct);
        var from = (dateFrom ?? DateTime.UtcNow.AddDays(-30)).ToString("yyyy-MM-dd");
        var to = (dateTo ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
        return File(bytes, ExcelContentType, $"lucratividade_{from}_{to}.xlsx");
    }

    [HttpGet("orders/excel")]
    public async Task<IActionResult> OrdersExcel(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var bytes = await _reportService.ExportOrdersToExcelAsync(dateFrom, dateTo, ct);
        var from = (dateFrom ?? DateTime.UtcNow.AddDays(-30)).ToString("yyyy-MM-dd");
        var to = (dateTo ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
        return File(bytes, ExcelContentType, $"vendas_{from}_{to}.xlsx");
    }

    [HttpGet("inventory/excel")]
    public async Task<IActionResult> InventoryExcel(CancellationToken ct = default)
    {
        var bytes = await _reportService.ExportInventoryToExcelAsync(ct);
        var fileName = $"estoque_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, ExcelContentType, fileName);
    }

    [HttpGet("accounting-export")]
    public async Task<IActionResult> AccountingExport(
        [FromQuery] string format = "bling",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var bytes = await _reportService.ExportAccountingAsync(format, dateFrom, dateTo, ct);
        var from = (dateFrom ?? DateTime.UtcNow.AddDays(-30)).ToString("yyyy-MM-dd");
        var to = (dateTo ?? DateTime.UtcNow).ToString("yyyy-MM-dd");
        var fileName = $"vendas_{format.ToLowerInvariant()}_{from}_{to}.csv";
        return File(bytes, "text/csv", fileName);
    }
}

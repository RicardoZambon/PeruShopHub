using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Inventory;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IServiceProvider _serviceProvider;

    public InventoryController(IInventoryService inventoryService, IServiceProvider serviceProvider)
    {
        _inventoryService = inventoryService;
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "productName",
        [FromQuery] string sortDir = "asc",
        CancellationToken ct = default)
    {
        var result = await _inventoryService.GetOverviewAsync(page, pageSize, search, sortBy, sortDir, ct);
        return Ok(result);
    }

    [HttpGet("movements")]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> GetMovements(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? variantId = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? createdBy = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _inventoryService.GetMovementsAsync(productId, variantId, type, dateFrom, dateTo, createdBy, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("movements/export")]
    public async Task<IActionResult> ExportMovements(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? variantId = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? createdBy = null,
        CancellationToken ct = default)
    {
        var bytes = await _inventoryService.ExportMovementsToExcelAsync(productId, variantId, type, dateFrom, dateTo, createdBy, ct);
        var fileName = $"movimentacoes_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<StockMovementDto>> Adjust([FromBody] StockAdjustmentDto dto, CancellationToken ct = default)
    {
        var result = await _inventoryService.CreateMovementAsync(dto, ct);
        return Ok(result);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<StockAlertDto>>> GetAlerts(CancellationToken ct = default)
    {
        var result = await _inventoryService.GetAlertsAsync(ct);
        return Ok(result);
    }

    [HttpPost("reconciliation")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<ReconciliationResultDto>> Reconcile(
        [FromBody] ReconciliationRequestDto dto,
        CancellationToken ct = default)
    {
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? "system";
        var result = await _inventoryService.ReconcileAsync(dto, userName, ct);
        return Ok(result);
    }

    [HttpGet("{productId}/allocations")]
    public async Task<ActionResult<ProductAllocationsDto>> GetAllocations(Guid productId, CancellationToken ct = default)
    {
        var result = await _inventoryService.GetAllocationsAsync(productId, ct);
        return Ok(result);
    }

    [HttpPut("{variantId}/allocations")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<StockAllocationDto>> UpdateAllocation(
        Guid variantId,
        [FromBody] UpdateStockAllocationDto dto,
        CancellationToken ct = default)
    {
        var result = await _inventoryService.UpdateAllocationAsync(variantId, dto, ct);
        return Ok(result);
    }

    /// <summary>Fetch ML Full (fulfillment) stock levels for an inventory/item.</summary>
    [HttpGet("{inventoryId}/stock/fulfillment")]
    public async Task<IActionResult> GetFulfillmentStock(string inventoryId, CancellationToken ct = default)
    {
        var adapter = _serviceProvider.GetRequiredKeyedService<IMarketplaceAdapter>("mercadolivre");
        var stock = await adapter.GetFulfillmentStockAsync(inventoryId, ct);
        return Ok(stock);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Inventory;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
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
        [FromQuery] string? type = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _inventoryService.GetMovementsAsync(productId, type, dateFrom, dateTo, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<StockMovementDto>> Adjust([FromBody] StockAdjustmentDto dto, CancellationToken ct = default)
    {
        var result = await _inventoryService.CreateMovementAsync(dto, ct);
        return Ok(result);
    }
}

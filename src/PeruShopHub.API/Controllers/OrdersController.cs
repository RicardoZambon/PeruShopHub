using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Orders;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderListDto>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string sortBy = "orderDate",
        [FromQuery] string sortDir = "desc",
        CancellationToken ct = default)
    {
        var result = await _orderService.GetListAsync(page, pageSize, search, status, dateFrom, dateTo, sortBy, sortDir, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(Guid id, CancellationToken ct = default)
    {
        var detail = await _orderService.GetByIdAsync(id, ct);
        return Ok(detail);
    }

    [HttpPost("{id:guid}/costs")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<OrderCostDto>> AddCost(Guid id, [FromBody] CreateOrderCostRequest request, CancellationToken ct = default)
    {
        var cost = await _orderService.AddCostAsync(id, request, ct);
        return CreatedAtAction(nameof(GetOrder), new { id }, cost);
    }

    [HttpPut("{id:guid}/costs/{costId:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<OrderCostDto>> UpdateCost(Guid id, Guid costId, [FromBody] UpdateOrderCostRequest request, CancellationToken ct = default)
    {
        var cost = await _orderService.UpdateCostAsync(id, costId, request, ct);
        return Ok(cost);
    }

    [HttpDelete("{id:guid}/costs/{costId:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteCost(Guid id, Guid costId, CancellationToken ct = default)
    {
        await _orderService.DeleteCostAsync(id, costId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/fulfill")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> FulfillOrder(Guid id, CancellationToken ct = default)
    {
        await _orderService.FulfillAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/recalculate-costs")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RecalculateCosts(Guid id, CancellationToken ct = default)
    {
        await _orderService.RecalculateCostsAsync(id, ct);
        return NoContent();
    }
}

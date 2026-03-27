using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.PurchaseOrders;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrdersController(IPurchaseOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? supplier = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDir = "desc",
        CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(page, pageSize, status, supplier, sortBy, sortDir, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var detail = await _service.GetByIdAsync(id, ct);
        return Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDetailDto>> Create([FromBody] CreatePurchaseOrderDto dto, CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> Update(Guid id, [FromBody] CreatePurchaseOrderDto dto, CancellationToken ct = default)
    {
        var updated = await _service.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> Receive(Guid id, CancellationToken ct = default)
    {
        var received = await _service.ReceiveAsync(id, ct);
        return Ok(received);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct = default)
    {
        await _service.CancelAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/costs")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> AddCost(Guid id, [FromBody] CreatePurchaseOrderCostDto dto, CancellationToken ct = default)
    {
        var result = await _service.AddCostAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/costs/{costId:guid}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> UpdateCost(Guid id, Guid costId, [FromBody] CreatePurchaseOrderCostDto dto, CancellationToken ct = default)
    {
        var result = await _service.UpdateCostAsync(id, costId, dto, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/costs/{costId:guid}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> RemoveCost(Guid id, Guid costId, CancellationToken ct = default)
    {
        var result = await _service.RemoveCostAsync(id, costId, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/cost-preview")]
    public async Task<ActionResult<CostDistributionPreviewDto>> CostPreview(
        Guid id,
        [FromQuery] decimal value,
        [FromQuery] string method = "by_value",
        CancellationToken ct = default)
    {
        var result = await _service.GetCostPreviewAsync(id, value, method, ct);
        return Ok(result);
    }
}

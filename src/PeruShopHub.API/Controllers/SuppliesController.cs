using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Supplies;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/supplies")]
[Authorize]
public class SuppliesController : ControllerBase
{
    private readonly ISupplyService _supplyService;

    public SuppliesController(ISupplyService supplyService)
    {
        _supplyService = supplyService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplyListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc",
        CancellationToken ct = default)
    {
        var result = await _supplyService.GetListAsync(page, pageSize, search, category, status, sortBy, sortDir, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplyDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _supplyService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<SupplyListDto>> Create([FromBody] CreateSupplyDto dto, CancellationToken ct = default)
    {
        var result = await _supplyService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<SupplyListDto>> Update(Guid id, [FromBody] UpdateSupplyDto dto, CancellationToken ct = default)
    {
        var result = await _supplyService.UpdateAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _supplyService.DeleteAsync(id, ct);
        return NoContent();
    }
}

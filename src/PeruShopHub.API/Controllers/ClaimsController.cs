using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Claims;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _service;

    public ClaimsController(IClaimService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ClaimListDto>>> GetClaims(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(status, type, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClaimDetailDto>> GetClaim(Guid id, CancellationToken ct = default)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/respond")]
    [Authorize(Roles = "Owner,Admin,Manager")]
    public async Task<ActionResult<ClaimDetailDto>> Respond(
        Guid id, [FromBody] RespondClaimRequest request, CancellationToken ct = default)
    {
        var result = await _service.RespondAsync(id, request, ct);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ClaimSummaryDto>> GetSummary(CancellationToken ct = default)
    {
        var result = await _service.GetSummaryAsync(ct);
        return Ok(result);
    }
}

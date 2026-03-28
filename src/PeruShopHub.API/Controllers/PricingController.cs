using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeruShopHub.Application.DTOs.Pricing;
using PeruShopHub.Application.Services;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/pricing")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricingService;

    public PricingController(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<PriceCalculationResult>> Calculate(
        [FromBody] PriceCalculationRequest request,
        CancellationToken ct = default)
    {
        var result = await _pricingService.CalculateAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<PricingRuleDto>>> GetRules(
        [FromQuery] Guid? productId = null,
        [FromQuery] string? marketplaceId = null,
        CancellationToken ct = default)
    {
        var rules = await _pricingService.GetRulesAsync(productId, marketplaceId, ct);
        return Ok(rules);
    }

    [HttpPost("rules")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<PricingRuleDto>> CreateRule(
        [FromBody] CreatePricingRuleDto dto,
        CancellationToken ct = default)
    {
        var rule = await _pricingService.CreateRuleAsync(dto, ct);
        return CreatedAtAction(nameof(GetRules), new { productId = rule.ProductId }, rule);
    }

    [HttpPut("rules/{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<PricingRuleDto>> UpdateRule(
        Guid id,
        [FromBody] UpdatePricingRuleDto dto,
        CancellationToken ct = default)
    {
        var rule = await _pricingService.UpdateRuleAsync(id, dto, ct);
        return Ok(rule);
    }

    [HttpDelete("rules/{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken ct = default)
    {
        await _pricingService.DeleteRuleAsync(id, ct);
        return NoContent();
    }
}

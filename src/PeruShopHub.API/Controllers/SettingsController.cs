using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Owner,Admin")]
public class SettingsController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    private static decimal _taxRate = 6.0m;

    public SettingsController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    // --- Integrations ---

    [HttpGet("integrations")]
    public async Task<ActionResult<IReadOnlyList<IntegrationDto>>> GetIntegrations()
    {
        var integrations = await _db.MarketplaceConnections
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new IntegrationDto(
                m.Id, m.MarketplaceId, m.Name, m.Logo,
                m.IsConnected, m.SellerNickname, m.LastSyncAt, m.ComingSoon))
            .ToListAsync();

        return Ok(integrations);
    }

    // --- Costs ---

    [HttpGet("costs")]
    public ActionResult<object> GetCosts()
    {
        var costs = new
        {
            defaultPackagingCost = 2.50m,
            icmsRate = 6.0m,
            taxRate = _taxRate,
            fixedCosts = new[]
            {
                new { id = "1", name = "Internet/Telefone", value = 150.00m },
                new { id = "2", name = "Software/Ferramentas", value = 89.90m }
            }
        };

        return Ok(costs);
    }

    [HttpPut("costs")]
    public ActionResult<object> UpdateCosts([FromBody] UpdateCostsDto dto)
    {
        if (dto.TaxRate.HasValue)
            _taxRate = dto.TaxRate.Value;

        return GetCosts();
    }

    // --- Commission Rules ---

    [HttpGet("commission-rules")]
    public async Task<ActionResult<IReadOnlyList<CommissionRuleDto>>> GetCommissionRules()
    {
        var rules = await _db.CommissionRules
            .AsNoTracking()
            .OrderBy(r => r.MarketplaceId)
            .ThenBy(r => r.CategoryPattern)
            .ThenBy(r => r.ListingType)
            .Select(r => new CommissionRuleDto(
                r.Id, r.MarketplaceId, r.CategoryPattern, r.ListingType, r.Rate, r.IsDefault))
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost("commission-rules")]
    public async Task<ActionResult<CommissionRuleDto>> CreateCommissionRule([FromBody] CreateCommissionRuleDto dto)
    {
        var rule = new CommissionRule
        {
            Id = Guid.NewGuid(),
            MarketplaceId = dto.MarketplaceId,
            CategoryPattern = dto.CategoryPattern,
            ListingType = dto.ListingType,
            Rate = dto.Rate,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.CommissionRules.Add(rule);
        await _db.SaveChangesAsync();

        var result = new CommissionRuleDto(
            rule.Id, rule.MarketplaceId, rule.CategoryPattern, rule.ListingType, rule.Rate, rule.IsDefault);

        return CreatedAtAction(nameof(GetCommissionRules), result);
    }

    [HttpPut("commission-rules/{id:guid}")]
    public async Task<ActionResult<CommissionRuleDto>> UpdateCommissionRule(Guid id, [FromBody] UpdateCommissionRuleDto dto)
    {
        var rule = await _db.CommissionRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        rule.Rate = dto.Rate;
        await _db.SaveChangesAsync();

        var result = new CommissionRuleDto(
            rule.Id, rule.MarketplaceId, rule.CategoryPattern, rule.ListingType, rule.Rate, rule.IsDefault);

        return Ok(result);
    }

    [HttpDelete("commission-rules/{id:guid}")]
    public async Task<IActionResult> DeleteCommissionRule(Guid id)
    {
        var rule = await _db.CommissionRules.FindAsync(id);
        if (rule is null)
            return NotFound();

        if (rule.IsDefault)
            return Conflict(new { message = "Cannot delete a default commission rule." });

        _db.CommissionRules.Remove(rule);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // --- Tax Profile ---

    [HttpGet("tax-profile")]
    public async Task<ActionResult<TaxProfileDto>> GetTaxProfile()
    {
        var profile = await _db.TaxProfiles.FirstOrDefaultAsync();

        if (profile is null)
        {
            // Return default values if no profile exists yet
            return Ok(new TaxProfileDto(Guid.Empty, "SimplesNacional", 6.0m, null));
        }

        return Ok(new TaxProfileDto(profile.Id, profile.TaxRegime, profile.AliquotPercentage, profile.State));
    }

    [HttpPut("tax-profile")]
    public async Task<ActionResult<TaxProfileDto>> UpdateTaxProfile([FromBody] UpdateTaxProfileDto dto)
    {
        var validRegimes = new[] { "SimplesNacional", "LucroPresumido", "MEI" };
        if (!validRegimes.Contains(dto.TaxRegime))
            return BadRequest(new { errors = new Dictionary<string, string[]> { ["TaxRegime"] = new[] { "Regime tributário inválido. Valores aceitos: SimplesNacional, LucroPresumido, MEI" } } });

        if (dto.AliquotPercentage < 0 || dto.AliquotPercentage > 100)
            return BadRequest(new { errors = new Dictionary<string, string[]> { ["AliquotPercentage"] = new[] { "Alíquota deve estar entre 0 e 100" } } });

        var profile = await _db.TaxProfiles.FirstOrDefaultAsync();

        if (profile is null)
        {
            profile = new TaxProfile
            {
                Id = Guid.NewGuid(),
                TaxRegime = dto.TaxRegime,
                AliquotPercentage = dto.AliquotPercentage,
                State = dto.State,
            };
            _db.TaxProfiles.Add(profile);
        }
        else
        {
            profile.TaxRegime = dto.TaxRegime;
            profile.AliquotPercentage = dto.AliquotPercentage;
            profile.State = dto.State;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new TaxProfileDto(profile.Id, profile.TaxRegime, profile.AliquotPercentage, profile.State));
    }
}

public record UpdateCostsDto(decimal? TaxRate);
public record UpdateCommissionRuleDto(decimal Rate);
public record TaxProfileDto(Guid Id, string TaxRegime, decimal AliquotPercentage, string? State);
public record UpdateTaxProfileDto(string TaxRegime, decimal AliquotPercentage, string? State);

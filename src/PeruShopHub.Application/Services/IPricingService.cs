using PeruShopHub.Application.DTOs.Pricing;

namespace PeruShopHub.Application.Services;

public interface IPricingService
{
    Task<PriceCalculationResult> CalculateAsync(PriceCalculationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PricingRuleDto>> GetRulesAsync(Guid? productId = null, string? marketplaceId = null, CancellationToken ct = default);
    Task<PricingRuleDto> CreateRuleAsync(CreatePricingRuleDto dto, CancellationToken ct = default);
    Task<PricingRuleDto> UpdateRuleAsync(Guid id, UpdatePricingRuleDto dto, CancellationToken ct = default);
    Task DeleteRuleAsync(Guid id, CancellationToken ct = default);
}

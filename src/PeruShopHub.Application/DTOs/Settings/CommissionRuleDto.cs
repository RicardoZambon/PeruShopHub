namespace PeruShopHub.Application.DTOs.Settings;

public record CommissionRuleDto(Guid Id, string MarketplaceId, string? CategoryPattern, string? ListingType, decimal Rate, bool IsDefault);
public record CreateCommissionRuleDto(string MarketplaceId, string? CategoryPattern, string? ListingType, decimal Rate);

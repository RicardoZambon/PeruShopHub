namespace PeruShopHub.Application.DTOs.Pricing;

public record PriceCalculationRequest(
    Guid ProductId,
    decimal TargetMarginPercent,
    string MarketplaceId = "mercadolivre",
    string? ListingType = null);

public record PriceCalculationResult(
    decimal SuggestedPrice,
    decimal ProductCost,
    decimal PackagingCost,
    decimal CommissionAmount,
    decimal CommissionRate,
    decimal TaxAmount,
    decimal TaxRate,
    decimal PaymentFeeAmount,
    decimal PaymentFeeRate,
    decimal TotalCosts,
    decimal ProfitAmount,
    decimal ActualMarginPercent,
    IReadOnlyList<CostComponentDto> CostBreakdown);

public record CostComponentDto(
    string Label,
    decimal Amount,
    decimal Percentage,
    string Color);

public record PricingRuleDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string MarketplaceId,
    string? ListingType,
    decimal TargetMarginPercent,
    decimal SuggestedPrice,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreatePricingRuleDto(
    Guid ProductId,
    string MarketplaceId,
    string? ListingType,
    decimal TargetMarginPercent);

public record UpdatePricingRuleDto(
    decimal TargetMarginPercent);

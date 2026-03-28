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

// --- Simulation DTOs ---

public record SimulationOverrides(
    decimal? ProductCost = null,
    decimal? PackagingCost = null,
    decimal? CommissionRate = null,
    decimal? TaxRate = null,
    decimal? PaymentFeeRate = null,
    decimal? ShippingCost = null,
    decimal? AdvertisingCost = null,
    decimal? Price = null);

public record SimulateRequest(
    Guid ProductId,
    SimulationOverrides Overrides,
    string MarketplaceId = "mercadolivre",
    string? ListingType = null);

public record BatchSimulateRequest(
    List<SimulateRequest> Items);

public record SimulationScenario(
    decimal Price,
    decimal ProductCost,
    decimal PackagingCost,
    decimal ShippingCost,
    decimal AdvertisingCost,
    decimal CommissionAmount,
    decimal CommissionRate,
    decimal TaxAmount,
    decimal TaxRate,
    decimal PaymentFeeAmount,
    decimal PaymentFeeRate,
    decimal TotalCosts,
    decimal ProfitAmount,
    decimal MarginPercent,
    IReadOnlyList<CostComponentDto> CostBreakdown);

public record SimulationResult(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    SimulationScenario Current,
    SimulationScenario Simulated,
    decimal MarginDiff,
    decimal ProfitDiff);

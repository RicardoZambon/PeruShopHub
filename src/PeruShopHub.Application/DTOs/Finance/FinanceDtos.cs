namespace PeruShopHub.Application.DTOs.Finance;

public record FinanceSummaryDto(
    decimal TotalRevenue,
    decimal TotalCosts,
    decimal TotalProfit,
    decimal AverageMargin,
    decimal RevenueChange,
    decimal ProfitChange,
    IReadOnlyList<CostBreakdownDto> CostBreakdown);

public record CostBreakdownDto(
    string Category,
    decimal Total,
    decimal Percentage);

public record SkuProfitabilityDto(
    Guid ProductId,
    string Sku,
    string Name,
    int UnitsSold,
    decimal Revenue,
    decimal TotalCosts,
    decimal Profit,
    decimal Margin);

public record ReconciliationDto(
    Guid OrderId,
    string ExternalOrderId,
    decimal ExpectedRevenue,
    decimal ActualRevenue,
    decimal Difference,
    string Status);

public record AbcProductDto(
    Guid ProductId,
    string Sku,
    string Name,
    decimal Revenue,
    decimal CumulativePercentage,
    string Classification);

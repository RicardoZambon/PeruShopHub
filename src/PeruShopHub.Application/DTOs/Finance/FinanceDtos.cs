namespace PeruShopHub.Application.DTOs.Finance;

public record FinanceSummaryDto(
    decimal TotalRevenue,
    decimal TotalCosts,
    decimal TotalProfit,
    decimal AverageMargin,
    decimal AverageTicket,
    decimal RevenueChange,
    decimal ProfitChange,
    IReadOnlyList<CostBreakdownDto> CostBreakdown);

public record CostBreakdownDto(
    string Category,
    decimal Total,
    decimal Percentage,
    string? Color = null);

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
    decimal Profit,
    decimal Margin,
    decimal CumulativePercentage,
    string Classification);

public record SkuProfitabilityDetailDto(
    Guid ProductId,
    string Sku,
    string Name,
    int UnitsSold,
    decimal Revenue,
    decimal Cmv,
    decimal Commissions,
    decimal Shipping,
    decimal Taxes,
    decimal TotalCosts,
    decimal Profit,
    decimal Margin);

public record MonthlyReconciliationDto(
    int Month,
    string MonthName,
    decimal ExpectedRevenue,
    decimal DepositedRevenue,
    decimal Difference);

public record MarginChartPointDto(
    string Label,
    decimal Margin);

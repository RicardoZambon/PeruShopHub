namespace PeruShopHub.Application.DTOs.Pricing;

public record FulfillmentCompareRequest(
    Guid ProductId,
    decimal? LaborCostPerShipment = null);

public record FulfillmentCompareResult(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    decimal FullCost,
    decimal OwnShippingCost,
    string Recommendation,
    decimal SavingsAmount,
    FulfillmentCostBreakdown FullBreakdown,
    OwnShippingCostBreakdown OwnBreakdown,
    decimal AvgDaysInStock,
    decimal DailyStorageCost,
    decimal FulfillmentFeePerSale,
    decimal AvgShippingCost,
    decimal PackagingCost,
    decimal LaborCostPerShipment);

public record FulfillmentCostBreakdown(
    decimal DailyStorageCost,
    decimal AvgDaysInStock,
    decimal StorageCostTotal,
    decimal FulfillmentFeePerSale,
    decimal Total);

public record OwnShippingCostBreakdown(
    decimal AvgShippingCost,
    decimal PackagingCost,
    decimal LaborCostPerShipment,
    decimal Total);

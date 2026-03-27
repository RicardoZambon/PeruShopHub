namespace PeruShopHub.Application.DTOs.PurchaseOrders;

public record PurchaseOrderListDto(Guid Id, string? Supplier, string Status, int ItemCount, decimal Total, DateTime CreatedAt, DateTime? ReceivedAt);

public record PurchaseOrderDetailDto(
    Guid Id, string? Supplier, string Status, string? Notes,
    decimal Subtotal, decimal AdditionalCosts, decimal Total,
    DateTime CreatedAt, DateTime? ReceivedAt,
    IReadOnlyList<PurchaseOrderItemDto> Items,
    IReadOnlyList<PurchaseOrderCostDto> Costs,
    int Version);

public record PurchaseOrderItemDto(
    Guid Id, Guid ProductId, Guid VariantId, string ProductName, string Sku,
    int Quantity, decimal UnitCost, decimal TotalCost,
    decimal AllocatedAdditionalCost, decimal EffectiveUnitCost);

public record PurchaseOrderCostDto(Guid Id, string Description, decimal Value, string DistributionMethod);

public record CreatePurchaseOrderDto(string? Supplier, string? Notes, List<CreatePurchaseOrderItemDto> Items, List<CreatePurchaseOrderCostDto>? Costs, int? Version = null);
public record CreatePurchaseOrderItemDto(Guid ProductId, Guid VariantId, int Quantity, decimal UnitCost);
public record CreatePurchaseOrderCostDto(string Description, decimal Value, string DistributionMethod);

public record CostDistributionPreviewDto(IReadOnlyList<ItemAllocationDto> Allocations);
public record ItemAllocationDto(Guid ItemId, string ProductName, string Sku, decimal AllocatedAmount, decimal EffectiveUnitCost);

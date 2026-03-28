namespace PeruShopHub.Application.DTOs.Inventory;

public record InventoryItemDto(string Sku, string ProductName, int TotalStock, int Reserved, int Available, decimal UnitCost, decimal StockValue);
public record StockMovementDto(Guid Id, string Sku, string ProductName, string Type, int Quantity, decimal? UnitCost, string? Reason, string? CreatedBy, DateTime CreatedAt);
public record StockAdjustmentDto(Guid ProductId, Guid VariantId, int Quantity, string Reason);

public record StockAllocationDto(
    Guid Id,
    Guid ProductVariantId,
    string VariantSku,
    string MarketplaceId,
    int AllocatedQuantity,
    int ReservedQuantity);

public record UpdateStockAllocationDto(
    string MarketplaceId,
    int AllocatedQuantity);

public record VariantAllocationsDto(
    Guid VariantId,
    string VariantSku,
    int TotalStock,
    int TotalAllocated,
    int Unallocated,
    List<StockAllocationDto> Allocations);

public record ProductAllocationsDto(
    Guid ProductId,
    string ProductName,
    List<VariantAllocationsDto> Variants);

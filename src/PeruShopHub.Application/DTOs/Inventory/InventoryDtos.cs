namespace PeruShopHub.Application.DTOs.Inventory;

public record InventoryItemDto(Guid ProductId, string Sku, string ProductName, int TotalStock, int Reserved, int Available, decimal UnitCost, decimal StockValue, int? MinStock = null, int? MaxStock = null);

public record StockAlertDto(Guid ProductId, string Sku, string ProductName, int TotalStock, int? MinStock, int Deficit);
public record StockMovementDto(Guid Id, string Sku, string ProductName, string Type, int Quantity, decimal? UnitCost, string? Reason, string? CreatedBy, DateTime CreatedAt, Guid? PurchaseOrderId = null, Guid? OrderId = null);
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

// Stock Reconciliation DTOs
public record ReconciliationItemDto(Guid VariantId, int CountedQuantity);

public record ReconciliationRequestDto(List<ReconciliationItemDto> Items);

public record ReconciliationResultItemDto(
    Guid VariantId,
    string Sku,
    string ProductName,
    int SystemQuantity,
    int CountedQuantity,
    int Difference,
    bool HasDiscrepancy);

public record ReconciliationResultDto(
    Guid BatchId,
    int ItemsChecked,
    int Discrepancies,
    int TotalDifference,
    DateTime ReconciliatedAt,
    List<ReconciliationResultItemDto> Items);

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

// ML Stock Reconciliation Report DTOs
public record ReconciliationReportDto(
    Guid Id,
    string MarketplaceId,
    int ItemsChecked,
    int Matches,
    int Discrepancies,
    int AutoCorrected,
    int ManualReviewRequired,
    string Status,
    string? ErrorMessage,
    DateTime StartedAt,
    DateTime? CompletedAt);

public record ReconciliationReportDetailDto(
    Guid Id,
    string MarketplaceId,
    int ItemsChecked,
    int Matches,
    int Discrepancies,
    int AutoCorrected,
    int ManualReviewRequired,
    string Status,
    string? ErrorMessage,
    DateTime StartedAt,
    DateTime? CompletedAt,
    List<ReconciliationReportItemDto> Items);

public record ReconciliationReportItemDto(
    Guid Id,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string ExternalId,
    int LocalQuantity,
    int MarketplaceQuantity,
    int Difference,
    string Resolution,
    string? Notes);

// Fulfillment (ML Full) Stock DTOs
public record FulfillmentStockItemDto(
    string ExternalId,
    string Sku,
    string ProductName,
    string? VariantName,
    int AvailableQuantity,
    int? NotAvailableQuantity,
    string? WarehouseId,
    string? Status);

public record ProductFulfillmentStockDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    List<FulfillmentStockItemDto> Items,
    int TotalAvailable,
    int TotalNotAvailable);

public record FulfillmentStockOverviewDto(
    List<ProductFulfillmentStockDto> Products,
    int TotalProducts,
    int TotalAvailable,
    int TotalNotAvailable);

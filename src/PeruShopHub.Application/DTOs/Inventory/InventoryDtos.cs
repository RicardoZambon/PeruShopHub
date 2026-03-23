namespace PeruShopHub.Application.DTOs.Inventory;

public record InventoryItemDto(string Sku, string ProductName, int TotalStock, int Reserved, int Available, decimal UnitCost, decimal StockValue);
public record StockMovementDto(Guid Id, string Sku, string ProductName, string Type, int Quantity, decimal? UnitCost, string? Reason, string? CreatedBy, DateTime CreatedAt);
public record StockAdjustmentDto(Guid ProductId, Guid VariantId, int Quantity, string Reason);

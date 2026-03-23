namespace PeruShopHub.Application.DTOs.Products;

public record ProductCostHistoryDto(Guid Id, DateTime Date, decimal PreviousCost, decimal NewCost, int Quantity, decimal UnitCostPaid, Guid? PurchaseOrderId, string Reason);

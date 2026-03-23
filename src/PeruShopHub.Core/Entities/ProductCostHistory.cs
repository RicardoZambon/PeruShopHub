namespace PeruShopHub.Core.Entities;

public class ProductCostHistory
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal PreviousCost { get; set; }
    public decimal NewCost { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCostPaid { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}

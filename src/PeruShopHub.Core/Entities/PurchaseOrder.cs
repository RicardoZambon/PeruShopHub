namespace PeruShopHub.Core.Entities;

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public string? Supplier { get; set; }
    public string Status { get; set; } = "Rascunho";
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal AdditionalCosts { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReceivedAt { get; set; }
    public int Version { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<PurchaseOrderCost> Costs { get; set; } = new List<PurchaseOrderCost>();
}

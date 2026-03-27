using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class StockMovement : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string Type { get; set; } = "Entrada";
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? OrderId { get; set; }
    public string? Reason { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}

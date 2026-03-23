namespace PeruShopHub.Core.Entities;

public class PurchaseOrderCost
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string DistributionMethod { get; set; } = "by_value";
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
}

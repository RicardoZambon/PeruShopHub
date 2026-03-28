using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class StockAllocation : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public string MarketplaceId { get; set; } = string.Empty;
    public int AllocatedQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ProductVariant Variant { get; set; } = null!;
}

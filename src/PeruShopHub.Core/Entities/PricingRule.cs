using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class PricingRule : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string MarketplaceId { get; set; } = "mercadolivre";
    public string? ListingType { get; set; }
    public decimal TargetMarginPercent { get; set; }
    public decimal SuggestedPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}

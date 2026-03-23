namespace PeruShopHub.Core.Entities;

public class CommissionRule
{
    public Guid Id { get; set; }
    public string MarketplaceId { get; set; } = "mercadolivre";
    public string? CategoryPattern { get; set; }
    public string? ListingType { get; set; }
    public decimal Rate { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

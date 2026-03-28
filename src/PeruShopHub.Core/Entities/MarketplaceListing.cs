using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

/// <summary>
/// Links a marketplace item (e.g., ML "MLB123456") to an internal product.
/// One product may have multiple listings across different marketplaces.
/// </summary>
public class MarketplaceListing : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string MarketplaceId { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? CategoryId { get; set; }
    public string? Permalink { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int AvailableQuantity { get; set; }
    public string? VariationsJson { get; set; }
    public string? PicturesJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Product? Product { get; set; }
}

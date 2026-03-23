namespace PeruShopHub.Core.Entities;

public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Attributes { get; set; } = "{}"; // JSON
    public decimal? Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public bool NeedsReview { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? Width { get; set; }
    public decimal? Length { get; set; }
    public Product Product { get; set; } = null!;
}

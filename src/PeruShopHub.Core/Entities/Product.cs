using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class Product : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CategoryId { get; set; }
    public decimal Price { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal PackagingCost { get; set; }
    public string? Supplier { get; set; }
    public string Status { get; set; } = "Ativo";
    public bool NeedsReview { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public decimal? StorageCostDaily { get; set; }
    public int? MinStock { get; set; }
    public int? MaxStock { get; set; }
    public int Version { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

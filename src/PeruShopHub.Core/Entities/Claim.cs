using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class MarketplaceClaim : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "claim", "return", "mediations"
    public string Status { get; set; } = "opened"; // opened, closed, etc.
    public string Reason { get; set; } = string.Empty;
    public string? BuyerComment { get; set; }
    public string? SellerComment { get; set; }
    public string? BuyerName { get; set; }
    public string? Resolution { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal? Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

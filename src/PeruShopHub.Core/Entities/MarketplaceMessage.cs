using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class MarketplaceMessage : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string ExternalPackId { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public string SenderType { get; set; } = string.Empty; // "seller" or "buyer"
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public string? ExternalMessageId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

namespace PeruShopHub.Core.Entities;

public class MarketplaceConnection
{
    public Guid Id { get; set; }
    public string MarketplaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public bool IsConnected { get; set; }
    public string? SellerNickname { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool ComingSoon { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

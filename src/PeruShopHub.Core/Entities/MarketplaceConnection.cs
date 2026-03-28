using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class MarketplaceConnection : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string MarketplaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public bool IsConnected { get; set; }
    public string? SellerNickname { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool ComingSoon { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // OAuth fields (encrypted at rest via IDataProtectionProvider)
    public string? AccessTokenProtected { get; set; }
    public string? RefreshTokenProtected { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public string? ExternalUserId { get; set; }
    public string Status { get; set; } = "Disconnected"; // Disconnected, Active, Expired, Error
}

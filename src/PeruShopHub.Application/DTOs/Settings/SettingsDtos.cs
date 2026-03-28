namespace PeruShopHub.Application.DTOs.Settings;

public record SystemUserDto(
    Guid Id,
    string Email,
    string Name,
    string Role,
    bool IsActive,
    DateTime? LastLogin,
    DateTime CreatedAt);

public record IntegrationDto(
    Guid Id,
    string MarketplaceId,
    string Name,
    string? Logo,
    bool IsConnected,
    string? SellerNickname,
    DateTime? LastSyncAt,
    bool ComingSoon,
    string Status,
    string? ExternalUserId,
    DateTime? TokenExpiresAt,
    int RefreshErrorCount);

public record CostConfigDto(
    string Category,
    string? Description,
    decimal? DefaultValue,
    string Source);

namespace PeruShopHub.Application.DTOs.Listings;

public record MarketplaceListingDto(
    Guid Id,
    string MarketplaceId,
    string ExternalId,
    Guid? ProductId,
    string Title,
    string Status,
    decimal Price,
    string? Permalink,
    string? ThumbnailUrl,
    int AvailableQuantity,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UnlinkedListingDto(
    Guid Id,
    string MarketplaceId,
    string ExternalId,
    string Title,
    string Status,
    decimal Price,
    string? ThumbnailUrl,
    int AvailableQuantity);

public record LinkMarketplaceDto(
    string MarketplaceId,
    Guid ListingId);

public record ProductListingDto(
    Guid ListingId,
    string MarketplaceId,
    string ExternalId,
    string Title,
    string Status,
    decimal Price,
    string? Permalink,
    string? ThumbnailUrl,
    int AvailableQuantity);

public record ListingGridDto(
    Guid Id,
    string MarketplaceId,
    string ExternalId,
    Guid? ProductId,
    string? ProductName,
    string Title,
    string Status,
    decimal Price,
    string? Permalink,
    string? ThumbnailUrl,
    int AvailableQuantity,
    string SyncStatus,
    DateTime UpdatedAt);

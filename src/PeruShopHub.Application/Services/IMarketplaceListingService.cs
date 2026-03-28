using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Listings;

namespace PeruShopHub.Application.Services;

public interface IMarketplaceListingService
{
    Task<PagedResult<ListingGridDto>> GetAllListingsAsync(
        string? search, string? status, string? syncStatus,
        string? sortBy, string? sortDirection,
        int page, int pageSize,
        CancellationToken ct = default);

    Task<PagedResult<UnlinkedListingDto>> GetUnlinkedListingsAsync(
        string marketplaceId, string? search, int page, int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductListingDto>> GetProductListingsAsync(
        Guid productId, CancellationToken ct = default);

    Task<ProductListingDto> LinkListingToProductAsync(
        Guid productId, LinkMarketplaceDto dto, CancellationToken ct = default);

    Task UnlinkListingFromProductAsync(
        Guid productId, string marketplaceId, CancellationToken ct = default);
}

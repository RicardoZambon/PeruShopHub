using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Listings;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class MarketplaceListingService : IMarketplaceListingService
{
    private readonly PeruShopHubDbContext _db;

    public MarketplaceListingService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UnlinkedListingDto>> GetUnlinkedListingsAsync(
        string marketplaceId, string? search, int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.MarketplaceListings
            .AsNoTracking()
            .Where(ml => ml.MarketplaceId == marketplaceId && ml.ProductId == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(ml =>
                ml.Title.ToLower().Contains(term) ||
                ml.ExternalId.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(ml => ml.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ml => new UnlinkedListingDto(
                ml.Id,
                ml.MarketplaceId,
                ml.ExternalId,
                ml.Title,
                ml.Status,
                ml.Price,
                ml.ThumbnailUrl,
                ml.AvailableQuantity))
            .ToListAsync(ct);

        return new PagedResult<UnlinkedListingDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<ProductListingDto>> GetProductListingsAsync(
        Guid productId, CancellationToken ct = default)
    {
        return await _db.MarketplaceListings
            .AsNoTracking()
            .Where(ml => ml.ProductId == productId)
            .OrderBy(ml => ml.MarketplaceId)
            .ThenBy(ml => ml.Title)
            .Select(ml => new ProductListingDto(
                ml.Id,
                ml.MarketplaceId,
                ml.ExternalId,
                ml.Title,
                ml.Status,
                ml.Price,
                ml.Permalink,
                ml.ThumbnailUrl,
                ml.AvailableQuantity))
            .ToListAsync(ct);
    }

    public async Task<ProductListingDto> LinkListingToProductAsync(
        Guid productId, LinkMarketplaceDto dto, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { productId }, ct)
            ?? throw new NotFoundException("Produto", productId);

        var listing = await _db.MarketplaceListings
            .FirstOrDefaultAsync(ml => ml.Id == dto.ListingId && ml.MarketplaceId == dto.MarketplaceId, ct)
            ?? throw new NotFoundException("Anúncio", dto.ListingId);

        if (listing.ProductId != null)
        {
            throw new ConflictException("Este anúncio já está vinculado a outro produto.");
        }

        listing.ProductId = productId;
        listing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new ProductListingDto(
            listing.Id,
            listing.MarketplaceId,
            listing.ExternalId,
            listing.Title,
            listing.Status,
            listing.Price,
            listing.Permalink,
            listing.ThumbnailUrl,
            listing.AvailableQuantity);
    }

    public async Task UnlinkListingFromProductAsync(
        Guid productId, string marketplaceId, CancellationToken ct = default)
    {
        var listing = await _db.MarketplaceListings
            .FirstOrDefaultAsync(ml => ml.ProductId == productId && ml.MarketplaceId == marketplaceId, ct)
            ?? throw new NotFoundException("Vínculo de marketplace não encontrado.");

        listing.ProductId = null;
        listing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

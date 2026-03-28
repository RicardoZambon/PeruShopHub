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

    public async Task<PagedResult<ListingGridDto>> GetAllListingsAsync(
        string? search, string? status, string? syncStatus,
        string? sortBy, string? sortDirection,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.MarketplaceListings
            .AsNoTracking()
            .Include(ml => ml.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(ml =>
                ml.Title.ToLower().Contains(term) ||
                ml.ExternalId.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(ml => ml.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(syncStatus))
        {
            query = syncStatus switch
            {
                "linked" => query.Where(ml => ml.ProductId != null),
                "unlinked" => query.Where(ml => ml.ProductId == null),
                _ => query
            };
        }

        var totalCount = await query.CountAsync(ct);

        query = (sortBy?.ToLower(), sortDirection?.ToLower()) switch
        {
            ("title", "desc") => query.OrderByDescending(ml => ml.Title),
            ("title", _) => query.OrderBy(ml => ml.Title),
            ("price", "desc") => query.OrderByDescending(ml => ml.Price),
            ("price", _) => query.OrderBy(ml => ml.Price),
            ("stock", "desc") => query.OrderByDescending(ml => ml.AvailableQuantity),
            ("stock", _) => query.OrderBy(ml => ml.AvailableQuantity),
            ("status", "desc") => query.OrderByDescending(ml => ml.Status),
            ("status", _) => query.OrderBy(ml => ml.Status),
            ("updatedAt", "desc") => query.OrderByDescending(ml => ml.UpdatedAt),
            ("updatedAt", _) => query.OrderBy(ml => ml.UpdatedAt),
            _ => query.OrderByDescending(ml => ml.UpdatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ml => new ListingGridDto(
                ml.Id,
                ml.MarketplaceId,
                ml.ExternalId,
                ml.ProductId,
                ml.Product != null ? ml.Product.Name : null,
                ml.Title,
                ml.Status,
                ml.Price,
                ml.Permalink,
                ml.ThumbnailUrl,
                ml.AvailableQuantity,
                ml.ProductId != null ? "Sincronizado" : "Não vinculado",
                ml.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<ListingGridDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
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

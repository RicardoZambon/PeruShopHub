using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Search;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class SearchService : ISearchService
{
    private readonly PeruShopHubDbContext _db;

    public SearchService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(
        string? query, int limit = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchResultDto>();

        var term = query.ToLower();
        var maxPerType = 3;

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term))
            .Take(maxPerType)
            .Select(p => new SearchResultDto(
                "produto", p.Id, p.Name, p.Sku, $"/products/{p.Id}"))
            .ToListAsync(ct);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.ExternalOrderId.ToLower().Contains(term) || o.BuyerName.ToLower().Contains(term))
            .Take(maxPerType)
            .Select(o => new SearchResultDto(
                "pedido", o.Id, o.ExternalOrderId, o.BuyerName, $"/sales/{o.Id}"))
            .ToListAsync(ct);

        var customers = await _db.Customers
            .AsNoTracking()
            .Where(c => c.Name.ToLower().Contains(term) || (c.Email != null && c.Email.ToLower().Contains(term)))
            .Take(maxPerType)
            .Select(c => new SearchResultDto(
                "cliente", c.Id, c.Name, c.Email, $"/customers/{c.Id}"))
            .ToListAsync(ct);

        return products
            .Concat(orders)
            .Concat(customers)
            .Take(limit)
            .ToList();
    }
}

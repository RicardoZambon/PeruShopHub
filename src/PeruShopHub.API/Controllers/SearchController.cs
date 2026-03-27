using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Search;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    public SearchController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SearchResultDto>>> Search(
        [FromQuery] string? q = null,
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<SearchResultDto>());

        var term = q.ToLower();
        var maxPerType = 3;

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term))
            .Take(maxPerType)
            .Select(p => new SearchResultDto(
                "produto", p.Id, p.Name, p.Sku, $"/products/{p.Id}"))
            .ToListAsync();

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.ExternalOrderId.ToLower().Contains(term) || o.BuyerName.ToLower().Contains(term))
            .Take(maxPerType)
            .Select(o => new SearchResultDto(
                "pedido", o.Id, o.ExternalOrderId, o.BuyerName, $"/sales/{o.Id}"))
            .ToListAsync();

        var customers = await _db.Customers
            .AsNoTracking()
            .Where(c => c.Name.ToLower().Contains(term) || (c.Email != null && c.Email.ToLower().Contains(term)))
            .Take(maxPerType)
            .Select(c => new SearchResultDto(
                "cliente", c.Id, c.Name, c.Email, $"/customers/{c.Id}"))
            .ToListAsync();

        var results = products
            .Concat(orders)
            .Concat(customers)
            .Take(limit)
            .ToList();

        return Ok(results);
    }
}

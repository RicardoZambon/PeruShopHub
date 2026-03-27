using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Customers;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly PeruShopHubDbContext _db;

    public CustomerService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CustomerListDto>> GetListAsync(
        int page, int pageSize, string? search,
        string sortBy, string sortDir,
        CancellationToken ct = default)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Nickname != null && c.Nickname.ToLower().Contains(term)) ||
                (c.Email != null && c.Email.ToLower().Contains(term)));
        }

        query = sortBy.ToLower() switch
        {
            "name" => sortDir == "asc" ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
            "totalorders" => sortDir == "asc" ? query.OrderBy(c => c.TotalOrders) : query.OrderByDescending(c => c.TotalOrders),
            "lastpurchase" => sortDir == "asc" ? query.OrderBy(c => c.LastPurchase) : query.OrderByDescending(c => c.LastPurchase),
            _ => sortDir == "asc" ? query.OrderBy(c => c.TotalSpent) : query.OrderByDescending(c => c.TotalSpent),
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListDto(
                c.Id, c.Name, c.Nickname, c.Email, c.Phone,
                c.TotalOrders, c.TotalSpent, c.LastPurchase))
            .ToListAsync(ct);

        return new PagedResult<CustomerListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Cliente", id);

        var recentOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == id)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .Select(o => new CustomerOrderDto(
                o.Id, o.ExternalOrderId, o.TotalAmount, o.Status, o.OrderDate))
            .ToListAsync(ct);

        return new CustomerDetailDto(
            customer.Id, customer.Name, customer.Nickname, customer.Email,
            customer.Phone, customer.TotalOrders, customer.TotalSpent,
            customer.LastPurchase, customer.CreatedAt, recentOrders);
    }
}

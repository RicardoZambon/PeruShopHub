using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Customers;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    public CustomersController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListDto>>> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "totalSpent",
        [FromQuery] string sortDir = "desc")
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

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListDto(
                c.Id,
                c.Name,
                c.Nickname,
                c.Email,
                c.Phone,
                c.TotalOrders,
                c.TotalSpent,
                c.LastPurchase))
            .ToListAsync();

        return Ok(new PagedResult<CustomerListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomer(Guid id)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null)
            return NotFound();

        var recentOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == id)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .Select(o => new CustomerOrderDto(
                o.Id,
                o.ExternalOrderId,
                o.TotalAmount,
                o.Status,
                o.OrderDate))
            .ToListAsync();

        var detail = new CustomerDetailDto(
            customer.Id,
            customer.Name,
            customer.Nickname,
            customer.Email,
            customer.Phone,
            customer.TotalOrders,
            customer.TotalSpent,
            customer.LastPurchase,
            customer.CreatedAt,
            recentOrders);

        return Ok(detail);
    }
}

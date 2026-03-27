using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Supplies;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/supplies")]
[Authorize]
public class SuppliesController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly INotificationDispatcher _dispatcher;

    public SuppliesController(PeruShopHubDbContext db, INotificationDispatcher dispatcher)
    {
        _db = db;
        _dispatcher = dispatcher;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplyListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc")
    {
        var query = _db.Supplies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Sku.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(s => s.Category == category);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        query = sortBy.ToLower() switch
        {
            "sku" => sortDir == "desc" ? query.OrderByDescending(s => s.Sku) : query.OrderBy(s => s.Sku),
            "unitcost" => sortDir == "desc" ? query.OrderByDescending(s => s.UnitCost) : query.OrderBy(s => s.UnitCost),
            "stock" => sortDir == "desc" ? query.OrderByDescending(s => s.Stock) : query.OrderBy(s => s.Stock),
            "category" => sortDir == "desc" ? query.OrderByDescending(s => s.Category) : query.OrderBy(s => s.Category),
            "status" => sortDir == "desc" ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            _ => sortDir == "desc" ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplyListDto(
                s.Id, s.Name, s.Sku, s.Category, s.UnitCost,
                s.Stock, s.MinimumStock, s.Supplier, s.Status))
            .ToListAsync();

        return Ok(new PagedResult<SupplyListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost]
    public async Task<ActionResult<SupplyListDto>> Create([FromBody] CreateSupplyDto dto)
    {
        var supply = new Supply
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Sku = dto.Sku,
            Category = dto.Category,
            UnitCost = dto.UnitCost,
            Stock = dto.Stock,
            MinimumStock = dto.MinimumStock,
            Supplier = dto.Supplier,
            Status = "Ativo",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Supplies.Add(supply);
        await _db.SaveChangesAsync();

        await _dispatcher.BroadcastDataChangeAsync("supply", "created", supply.Id.ToString(), default);

        var result = new SupplyListDto(
            supply.Id, supply.Name, supply.Sku, supply.Category,
            supply.UnitCost, supply.Stock, supply.MinimumStock,
            supply.Supplier, supply.Status);

        return CreatedAtAction(nameof(GetAll), new { id = supply.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplyListDto>> Update(Guid id, [FromBody] UpdateSupplyDto dto)
    {
        var supply = await _db.Supplies.FindAsync(id);
        if (supply is null)
            return NotFound();

        if (dto.Name is not null) supply.Name = dto.Name;
        if (dto.Sku is not null) supply.Sku = dto.Sku;
        if (dto.Category is not null) supply.Category = dto.Category;
        if (dto.UnitCost.HasValue) supply.UnitCost = dto.UnitCost.Value;
        if (dto.Stock.HasValue) supply.Stock = dto.Stock.Value;
        if (dto.MinimumStock.HasValue) supply.MinimumStock = dto.MinimumStock.Value;
        if (dto.Supplier is not null) supply.Supplier = dto.Supplier;
        if (dto.Status is not null) supply.Status = dto.Status;
        supply.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _dispatcher.BroadcastDataChangeAsync("supply", "updated", supply.Id.ToString(), default);

        var result = new SupplyListDto(
            supply.Id, supply.Name, supply.Sku, supply.Category,
            supply.UnitCost, supply.Stock, supply.MinimumStock,
            supply.Supplier, supply.Status);

        return Ok(result);
    }
}

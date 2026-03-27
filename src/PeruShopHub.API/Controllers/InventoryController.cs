using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Inventory;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    public InventoryController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "productName",
        [FromQuery] string sortDir = "asc")
    {
        var query = _db.Products.AsNoTracking()
            .Include(p => p.Variants)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Sku.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync();

        var projected = query.Select(p => new InventoryItemDto(
            p.Sku,
            p.Name,
            p.Variants.Sum(v => v.Stock),
            0,
            p.Variants.Sum(v => v.Stock),
            p.PurchaseCost,
            p.Variants.Sum(v => v.Stock) * p.PurchaseCost));

        var desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        projected = sortBy.ToLower() switch
        {
            "sku" => desc ? projected.OrderByDescending(i => i.Sku) : projected.OrderBy(i => i.Sku),
            "totalstock" => desc ? projected.OrderByDescending(i => i.TotalStock) : projected.OrderBy(i => i.TotalStock),
            "reserved" => desc ? projected.OrderByDescending(i => i.Reserved) : projected.OrderBy(i => i.Reserved),
            "available" => desc ? projected.OrderByDescending(i => i.Available) : projected.OrderBy(i => i.Available),
            "unitcost" => desc ? projected.OrderByDescending(i => i.UnitCost) : projected.OrderBy(i => i.UnitCost),
            "stockvalue" => desc ? projected.OrderByDescending(i => i.StockValue) : projected.OrderBy(i => i.StockValue),
            _ => desc ? projected.OrderByDescending(i => i.ProductName) : projected.OrderBy(i => i.ProductName),
        };

        var items = await projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<InventoryItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("movements")]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> GetMovements(
        [FromQuery] Guid? productId = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.StockMovements.AsNoTracking()
            .Include(m => m.Product)
            .Include(m => m.Variant)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(m => m.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(m => m.Type == type);

        if (dateFrom.HasValue)
            query = query.Where(m => m.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(m => m.CreatedAt <= dateTo.Value);

        query = query.OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new StockMovementDto(
                m.Id,
                m.Variant != null ? m.Variant.Sku : m.Product.Sku,
                m.Product.Name,
                m.Type,
                m.Quantity,
                m.UnitCost,
                m.Reason,
                m.CreatedBy,
                m.CreatedAt))
            .ToListAsync();

        return Ok(new PagedResult<StockMovementDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost("adjust")]
    public async Task<ActionResult<StockMovementDto>> Adjust([FromBody] StockAdjustmentDto dto)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == dto.VariantId && v.ProductId == dto.ProductId);

        if (variant is null)
            return NotFound(new { message = "Variante do produto não encontrada." });

        variant.Stock += dto.Quantity;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            VariantId = dto.VariantId,
            Type = "Ajuste",
            Quantity = dto.Quantity,
            UnitCost = variant.PurchaseCost ?? variant.Product.PurchaseCost,
            Reason = dto.Reason,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };

        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();

        var result = new StockMovementDto(
            movement.Id,
            variant.Sku,
            variant.Product.Name,
            movement.Type,
            movement.Quantity,
            movement.UnitCost,
            movement.Reason,
            movement.CreatedBy,
            movement.CreatedAt);

        return Ok(result);
    }
}

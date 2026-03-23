using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Inventory;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;

    public InventoryController(PeruShopHubDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryItemDto>>> GetAll()
    {
        var items = await _db.Products.AsNoTracking()
            .Include(p => p.Variants)
            .Where(p => p.IsActive)
            .Select(p => new InventoryItemDto(
                p.Sku,
                p.Name,
                p.Variants.Sum(v => v.Stock),
                0, // Reserved (placeholder for now)
                p.Variants.Sum(v => v.Stock), // Available = TotalStock - Reserved
                p.PurchaseCost,
                p.Variants.Sum(v => v.Stock) * p.PurchaseCost))
            .ToListAsync();

        return Ok(items);
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

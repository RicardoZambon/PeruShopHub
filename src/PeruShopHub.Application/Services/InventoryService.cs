using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Inventory;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly PeruShopHubDbContext _db;

    public InventoryService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<InventoryItemDto>> GetOverviewAsync(
        int page, int pageSize, string? search,
        string sortBy, string sortDir,
        CancellationToken ct = default)
    {
        var query = _db.Products.AsNoTracking()
            .Include(p => p.Variants)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Sku.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(ct);

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
            .ToListAsync(ct);

        return new PagedResult<InventoryItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<StockMovementDto>> GetMovementsAsync(
        Guid? productId, string? type,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize,
        CancellationToken ct = default)
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

        var totalCount = await query.CountAsync(ct);
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
            .ToListAsync(ct);

        return new PagedResult<StockMovementDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StockMovementDto> CreateMovementAsync(StockAdjustmentDto dto, CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == dto.VariantId && v.ProductId == dto.ProductId, ct)
            ?? throw new NotFoundException("Variante do produto não encontrada.");

        variant.Stock += dto.Quantity;

        // Adjust allocations if new stock is below total allocated
        await AdjustAllocationsIfExceedingAsync(variant.Id, variant.Stock, ct);

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
        await _db.SaveChangesAsync(ct);

        return new StockMovementDto(
            movement.Id,
            variant.Sku,
            variant.Product.Name,
            movement.Type,
            movement.Quantity,
            movement.UnitCost,
            movement.Reason,
            movement.CreatedBy,
            movement.CreatedAt);
    }

    public async Task<ProductAllocationsDto> GetAllocationsAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new NotFoundException("Produto não encontrado.");

        var variantIds = product.Variants.Select(v => v.Id).ToList();

        var allocations = await _db.StockAllocations.AsNoTracking()
            .Where(a => variantIds.Contains(a.ProductVariantId))
            .ToListAsync(ct);

        var variantDtos = product.Variants.Select(v =>
        {
            var variantAllocations = allocations
                .Where(a => a.ProductVariantId == v.Id)
                .Select(a => new StockAllocationDto(
                    a.Id,
                    a.ProductVariantId,
                    v.Sku,
                    a.MarketplaceId,
                    a.AllocatedQuantity,
                    a.ReservedQuantity))
                .ToList();

            var totalAllocated = variantAllocations.Sum(a => a.AllocatedQuantity);

            return new VariantAllocationsDto(
                v.Id,
                v.Sku,
                v.Stock,
                totalAllocated,
                v.Stock - totalAllocated,
                variantAllocations);
        }).ToList();

        return new ProductAllocationsDto(product.Id, product.Name, variantDtos);
    }

    public async Task<StockAllocationDto> UpdateAllocationAsync(Guid variantId, UpdateStockAllocationDto dto, CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == variantId, ct)
            ?? throw new NotFoundException("Variante do produto não encontrada.");

        if (dto.AllocatedQuantity < 0)
            throw new AppValidationException("AllocatedQuantity", "Quantidade alocada não pode ser negativa.");

        // Check total allocations for this variant (excluding the marketplace being updated)
        var existingAllocations = await _db.StockAllocations
            .Where(a => a.ProductVariantId == variantId && a.MarketplaceId != dto.MarketplaceId)
            .SumAsync(a => a.AllocatedQuantity, ct);

        if (existingAllocations + dto.AllocatedQuantity > variant.Stock)
            throw new AppValidationException("AllocatedQuantity",
                $"Soma das alocações ({existingAllocations + dto.AllocatedQuantity}) excede o estoque total ({variant.Stock}).");

        var allocation = await _db.StockAllocations
            .FirstOrDefaultAsync(a => a.ProductVariantId == variantId && a.MarketplaceId == dto.MarketplaceId, ct);

        if (allocation == null)
        {
            allocation = new StockAllocation
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variantId,
                MarketplaceId = dto.MarketplaceId,
                AllocatedQuantity = dto.AllocatedQuantity,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.StockAllocations.Add(allocation);
        }
        else
        {
            allocation.AllocatedQuantity = dto.AllocatedQuantity;
            allocation.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return new StockAllocationDto(
            allocation.Id,
            allocation.ProductVariantId,
            variant.Sku,
            allocation.MarketplaceId,
            allocation.AllocatedQuantity,
            allocation.ReservedQuantity);
    }

    private async Task AdjustAllocationsIfExceedingAsync(Guid variantId, int newStock, CancellationToken ct)
    {
        var allocations = await _db.StockAllocations
            .Where(a => a.ProductVariantId == variantId)
            .ToListAsync(ct);

        if (allocations.Count == 0) return;

        var totalAllocated = allocations.Sum(a => a.AllocatedQuantity);
        if (totalAllocated <= newStock) return;

        // Proportionally reduce allocations to fit new stock
        var targetTotal = Math.Max(0, newStock);
        foreach (var alloc in allocations)
        {
            alloc.AllocatedQuantity = totalAllocated > 0
                ? (int)Math.Floor((double)alloc.AllocatedQuantity / totalAllocated * targetTotal)
                : 0;
            alloc.UpdatedAt = DateTime.UtcNow;
        }

        // Distribute remainder due to rounding to the largest allocation
        var distributed = allocations.Sum(a => a.AllocatedQuantity);
        var remainder = targetTotal - distributed;
        if (remainder > 0 && allocations.Count > 0)
        {
            allocations.OrderByDescending(a => a.AllocatedQuantity).First().AllocatedQuantity += remainder;
        }
    }
}

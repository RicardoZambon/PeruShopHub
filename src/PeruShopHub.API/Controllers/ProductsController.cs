using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICacheService _cache;
    private readonly INotificationDispatcher _dispatcher;

    public ProductsController(PeruShopHubDbContext db, ICacheService cache, INotificationDispatcher dispatcher)
    {
        _db = db;
        _cache = cache;
        _dispatcher = dispatcher;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc",
        CancellationToken ct = default)
    {
        var cacheKey = $"products:list:{page}:{pageSize}:{search}:{status}:{categoryId}:{sortBy}:{sortDir}";
        var cached = await _cache.GetAsync<PagedResult<ProductListDto>>(cacheKey, ct);
        if (cached is not null) return Ok(cached);

        var query = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Sku.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (categoryId.HasValue)
        {
            // Get all descendant category IDs recursively
            var categoryIds = new List<Guid> { categoryId.Value };
            var toCheck = new Queue<Guid>();
            toCheck.Enqueue(categoryId.Value);
            while (toCheck.Count > 0)
            {
                var parentId = toCheck.Dequeue();
                var childIds = await _db.Categories
                    .AsNoTracking()
                    .Where(c => c.ParentId == parentId)
                    .Select(c => c.Id)
                    .ToListAsync(ct);
                foreach (var childId in childIds)
                {
                    categoryIds.Add(childId);
                    toCheck.Enqueue(childId);
                }
            }
            var categoryIdStrings = categoryIds.Select(id => id.ToString()).ToList();
            query = query.Where(p => p.CategoryId != null && categoryIdStrings.Contains(p.CategoryId));
        }

        var totalCount = await query.CountAsync();

        query = sortBy.ToLower() switch
        {
            "sku" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(p => p.Sku)
                : query.OrderBy(p => p.Sku),
            "price" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(p => p.Price)
                : query.OrderBy(p => p.Price),
            "status" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(p => p.Status)
                : query.OrderBy(p => p.Status),
            "createdat" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(p => p.CreatedAt)
                : query.OrderBy(p => p.CreatedAt),
            "stock" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(p => p.Variants.Sum(v => v.Stock))
                : query.OrderBy(p => p.Variants.Sum(v => v.Stock)),
            "margin" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(p => p.Price > 0 ? (p.Price - p.PurchaseCost - p.PackagingCost) / p.Price * 100 : 0)
                : query.OrderBy(p => p.Price > 0 ? (p.Price - p.PurchaseCost - p.PackagingCost) / p.Price * 100 : 0),
            _ => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
        };

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Price,
                p.PurchaseCost,
                p.PackagingCost,
                p.Status,
                p.NeedsReview,
                p.IsActive,
                p.Variants.Count,
                p.Variants.Sum(v => v.Stock),
                p.Price > 0
                    ? (p.Price - p.PurchaseCost - p.PackagingCost) / p.Price * 100m
                    : (decimal?)null,
                _db.FileUploads
                    .Where(f => f.EntityType == "product" && f.EntityId == p.Id)
                    .OrderBy(f => f.SortOrder)
                    .Select(f => f.StoragePath)
                    .FirstOrDefault(),
                p.CreatedAt))
            .ToListAsync(ct);

        var result = new PagedResult<ProductListDto>
        {
            Items = products,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(60), ct);
        return Ok(result);
    }

    [HttpGet("next-sku")]
    public async Task<ActionResult> GetNextSku([FromQuery] Guid? categoryId)
    {
        if (!categoryId.HasValue)
            return Ok(new { suggestedSku = (string?)null });

        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId.Value);

        if (category?.SkuPrefix is null or "")
            return Ok(new { suggestedSku = (string?)null });

        var prefix = category.SkuPrefix;
        var pattern = $"{prefix}-";

        // Find the max existing SKU with this prefix
        var maxSku = await _db.Products
            .AsNoTracking()
            .Where(p => p.Sku.StartsWith(pattern))
            .Select(p => p.Sku)
            .OrderByDescending(s => s)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (maxSku is not null)
        {
            var suffix = maxSku[(pattern.Length)..];
            if (int.TryParse(suffix, out var parsed))
            {
                nextNumber = parsed + 1;
            }
        }

        var suggestedSku = $"{prefix}-{nextNumber:D3}";
        return Ok(new { suggestedSku });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(Guid id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();

        if (product is null)
            return NotFound();

        var photoUrls = await _db.FileUploads
            .AsNoTracking()
            .Where(f => f.EntityType == "product" && f.EntityId == id)
            .OrderBy(f => f.SortOrder)
            .Select(f => f.StoragePath)
            .ToListAsync();

        var dto = new ProductDetailDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.CategoryId,
            product.Price,
            product.PurchaseCost,
            product.PackagingCost,
            product.Supplier,
            product.Status,
            product.NeedsReview,
            product.IsActive,
            product.Weight,
            product.Height,
            product.Width,
            product.Length,
            product.CreatedAt,
            product.UpdatedAt,
            product.Variants.Select(v => new ProductVariantDto(
                v.Id,
                v.Sku,
                v.Attributes,
                v.Price,
                v.Stock,
                v.IsActive,
                v.NeedsReview,
                v.PurchaseCost,
                v.Weight,
                v.Height,
                v.Width,
                v.Length)).ToList(),
            photoUrls);

        return Ok(dto);
    }

    [HttpGet("{id:guid}/variants")]
    public async Task<ActionResult<IReadOnlyList<ProductVariantDto>>> GetVariants(Guid id)
    {
        var productExists = await _db.Products.AsNoTracking().AnyAsync(p => p.Id == id);
        if (!productExists)
            return NotFound();

        var variants = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == id)
            .Select(v => new ProductVariantDto(
                v.Id,
                v.Sku,
                v.Attributes,
                v.Price,
                v.Stock,
                v.IsActive,
                v.NeedsReview,
                v.PurchaseCost,
                v.Weight,
                v.Height,
                v.Width,
                v.Length))
            .ToListAsync();

        return Ok(variants);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailDto>> CreateProduct(CreateProductDto dto)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = dto.Sku,
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            Price = dto.Price,
            PurchaseCost = dto.PurchaseCost,
            PackagingCost = dto.PackagingCost,
            Supplier = dto.Supplier,
            Weight = dto.Weight,
            Height = dto.Height,
            Width = dto.Width,
            Length = dto.Length,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await _dispatcher.BroadcastDataChangeAsync("product", "created", product.Id.ToString(), default);

        var createResult = new ProductDetailDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.CategoryId,
            product.Price,
            product.PurchaseCost,
            product.PackagingCost,
            product.Supplier,
            product.Status,
            product.NeedsReview,
            product.IsActive,
            product.Weight,
            product.Height,
            product.Width,
            product.Length,
            product.CreatedAt,
            product.UpdatedAt,
            Array.Empty<ProductVariantDto>(),
            Array.Empty<string>());

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, createResult);
    }

    [HttpGet("{id:guid}/cost-history")]
    public async Task<ActionResult<PagedResult<ProductCostHistoryDto>>> GetCostHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var productExists = await _db.Products.AsNoTracking().AnyAsync(p => p.Id == id);
        if (!productExists)
            return NotFound();

        var query = _db.ProductCostHistories
            .AsNoTracking()
            .Where(h => h.ProductId == id);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new ProductCostHistoryDto(
                h.Id,
                h.CreatedAt,
                h.PreviousCost,
                h.NewCost,
                h.Quantity,
                h.UnitCostPaid,
                h.PurchaseOrderId,
                h.Reason))
            .ToListAsync();

        var result = new PagedResult<ProductCostHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDetailDto>> UpdateProduct(Guid id, UpdateProductDto dto)
    {
        var product = await _db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        if (dto.Sku is not null) product.Sku = dto.Sku;
        if (dto.Name is not null) product.Name = dto.Name;
        if (dto.Description is not null) product.Description = dto.Description;
        if (dto.CategoryId is not null) product.CategoryId = dto.CategoryId;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.PurchaseCost.HasValue) product.PurchaseCost = dto.PurchaseCost.Value;
        if (dto.PackagingCost.HasValue) product.PackagingCost = dto.PackagingCost.Value;
        if (dto.Supplier is not null) product.Supplier = dto.Supplier;
        if (dto.Status is not null) product.Status = dto.Status;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
        if (dto.Weight.HasValue) product.Weight = dto.Weight.Value;
        if (dto.Height.HasValue) product.Height = dto.Height.Value;
        if (dto.Width.HasValue) product.Width = dto.Width.Value;
        if (dto.Length.HasValue) product.Length = dto.Length.Value;

        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _dispatcher.BroadcastDataChangeAsync("product", "updated", product.Id.ToString(), default);

        var photoUrls = await _db.FileUploads
            .AsNoTracking()
            .Where(f => f.EntityType == "product" && f.EntityId == id)
            .OrderBy(f => f.SortOrder)
            .Select(f => f.StoragePath)
            .ToListAsync();

        var result = new ProductDetailDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.CategoryId,
            product.Price,
            product.PurchaseCost,
            product.PackagingCost,
            product.Supplier,
            product.Status,
            product.NeedsReview,
            product.IsActive,
            product.Weight,
            product.Height,
            product.Width,
            product.Length,
            product.CreatedAt,
            product.UpdatedAt,
            product.Variants.Select(v => new ProductVariantDto(
                v.Id,
                v.Sku,
                v.Attributes,
                v.Price,
                v.Stock,
                v.IsActive,
                v.NeedsReview,
                v.PurchaseCost,
                v.Weight,
                v.Height,
                v.Width,
                v.Length)).ToList(),
            photoUrls);

        return Ok(result);
    }

    [HttpPost("{id:guid}/variants")]
    public async Task<ActionResult<ProductVariantDto>> CreateVariant(Guid id, CreateProductVariantDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = id,
            Sku = dto.Sku,
            Attributes = dto.Attributes,
            Price = dto.Price,
            Stock = 0,
            IsActive = dto.IsActive,
        };

        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync();

        return Ok(MapToVariantDto(variant));
    }

    [HttpPut("{id:guid}/variants/{variantId:guid}")]
    public async Task<ActionResult<ProductVariantDto>> UpdateVariant(Guid id, Guid variantId, UpdateProductVariantDto dto)
    {
        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == id);
        if (variant is null) return NotFound();

        if (dto.Sku is not null) variant.Sku = dto.Sku;
        if (dto.Price.HasValue) variant.Price = dto.Price;
        if (dto.IsActive.HasValue) variant.IsActive = dto.IsActive.Value;
        if (dto.PurchaseCost.HasValue) variant.PurchaseCost = dto.PurchaseCost;
        if (dto.Weight.HasValue) variant.Weight = dto.Weight;
        if (dto.Height.HasValue) variant.Height = dto.Height;
        if (dto.Width.HasValue) variant.Width = dto.Width;
        if (dto.Length.HasValue) variant.Length = dto.Length;

        await _db.SaveChangesAsync();
        return Ok(MapToVariantDto(variant));
    }

    [HttpDelete("{id:guid}/variants/{variantId:guid}")]
    public async Task<IActionResult> DeleteVariant(Guid id, Guid variantId)
    {
        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == id);
        if (variant is null) return NotFound();

        _db.ProductVariants.Remove(variant);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        // Check if product has order history
        var hasOrders = await _db.OrderItems.AnyAsync(oi => oi.ProductId == id);

        if (hasOrders)
        {
            // Soft delete: deactivate and set status
            product.IsActive = false;
            product.Status = "Excluído";
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        else
        {
            // Hard delete: remove product and its variants
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
        }

        await _dispatcher.BroadcastDataChangeAsync("product", "deleted", id.ToString(), default);
        return NoContent();
    }

    private static ProductVariantDto MapToVariantDto(ProductVariant v) => new(
        v.Id,
        v.Sku,
        v.Attributes,
        v.Price,
        v.Stock,
        v.IsActive,
        v.NeedsReview,
        v.PurchaseCost,
        v.Weight,
        v.Height,
        v.Width,
        v.Length);
}

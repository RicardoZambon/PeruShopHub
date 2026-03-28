using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class ProductService : IProductService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICacheService _cache;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IAuditService _auditService;

    public ProductService(PeruShopHubDbContext db, ICacheService cache, INotificationDispatcher dispatcher, IAuditService auditService)
    {
        _db = db;
        _cache = cache;
        _dispatcher = dispatcher;
        _auditService = auditService;
    }

    public async Task<PagedResult<ProductListDto>> GetListAsync(
        int page, int pageSize, string? search, string? status,
        Guid? categoryId, string sortBy, string sortDir,
        CancellationToken ct = default)
    {
        var cacheKey = $"products:list:{page}:{pageSize}:{search}:{status}:{categoryId}:{sortBy}:{sortDir}";
        var cached = await _cache.GetAsync<PagedResult<ProductListDto>>(cacheKey, ct);
        if (cached is not null) return cached;

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
            var categoryIds = await GetDescendantCategoryIdsAsync(categoryId.Value, ct);
            var categoryIdStrings = categoryIds.Select(id => id.ToString()).ToList();
            query = query.Where(p => p.CategoryId != null && categoryIdStrings.Contains(p.CategoryId));
        }

        var totalCount = await query.CountAsync(ct);

        query = ApplySorting(query, sortBy, sortDir);

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
                p.StorageCostDaily,
                p.Status,
                p.NeedsReview,
                p.IsActive,
                p.Variants.Count,
                p.Variants.Sum(v => v.Stock),
                p.Price > 0
                    ? (p.Price - p.PurchaseCost - p.PackagingCost) / p.Price * 100m
                    : (decimal?)null,
                _db.FileUploads
                    .Where(f => f.EntityType == "product" && f.EntityId == p.Id && f.IsActive)
                    .OrderBy(f => f.SortOrder)
                    .Select(f => f.StoragePath)
                    .FirstOrDefault(),
                p.CreatedAt,
                p.MinStock,
                p.MaxStock,
                (string?)null,
                false))
            .ToListAsync(ct);

        // Enrich with ABC classification from materialized view
        var productIds = products.Select(p => p.Id).ToList();
        var abcLookup = await GetAbcClassificationsAsync(productIds, ct);

        // Enrich with marketplace listing presence
        var listingLookup = await _db.MarketplaceListings
            .AsNoTracking()
            .Where(ml => ml.ProductId != null && productIds.Contains(ml.ProductId.Value))
            .Select(ml => ml.ProductId!.Value)
            .Distinct()
            .ToListAsync(ct);
        var listingSet = new HashSet<Guid>(listingLookup);

        var enriched = products.Select(p =>
            p with {
                AbcClass = abcLookup.GetValueOrDefault(p.Id),
                HasMarketplaceListing = listingSet.Contains(p.Id)
            }).ToList();

        var result = new PagedResult<ProductListDto>
        {
            Items = enriched,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(60), ct);
        return result;
    }

    private async Task<Dictionary<Guid, string>> GetAbcClassificationsAsync(
        List<Guid> productIds, CancellationToken ct)
    {
        if (productIds.Count == 0) return new();

        try
        {
            // Get all SKU profitability data to compute ABC (materialized view)
            var allSkus = await _db.SkuProfitabilityViews
                .OrderByDescending(v => v.TotalRevenue)
                .Select(v => new { v.ProductId, v.TotalRevenue })
                .ToListAsync(ct);

            var totalRevenue = allSkus.Sum(v => v.TotalRevenue);
            if (totalRevenue == 0) return new();

            var cumulative = 0m;
            var classifications = new Dictionary<Guid, string>();

            foreach (var sku in allSkus)
            {
                if (sku.ProductId is null) continue;
                var pid = sku.ProductId.Value;

                cumulative += sku.TotalRevenue;
                var pct = cumulative / totalRevenue * 100m;
                var cls = pct <= 80 ? "A" : pct <= 95 ? "B" : "C";

                if (productIds.Contains(pid) && !classifications.ContainsKey(pid))
                    classifications[pid] = cls;
            }

            return classifications;
        }
        catch
        {
            // Materialized view may not exist (e.g., in test environments)
            return new();
        }
    }

    public async Task<ProductDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync(ct);

        if (product is null)
            throw new NotFoundException("Produto", id);

        var photoUrls = await GetPhotoUrlsAsync(id, ct);

        return MapToDetailDto(product, photoUrls);
    }

    public async Task<string?> GetNextSkuAsync(Guid? categoryId, CancellationToken ct = default)
    {
        if (!categoryId.HasValue)
            return null;

        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId.Value, ct);

        if (category?.SkuPrefix is null or "")
            return null;

        var prefix = category.SkuPrefix;
        var pattern = $"{prefix}-";

        var maxSku = await _db.Products
            .AsNoTracking()
            .Where(p => p.Sku.StartsWith(pattern))
            .Select(p => p.Sku)
            .OrderByDescending(s => s)
            .FirstOrDefaultAsync(ct);

        int nextNumber = 1;
        if (maxSku is not null)
        {
            var suffix = maxSku[(pattern.Length)..];
            if (int.TryParse(suffix, out var parsed))
            {
                nextNumber = parsed + 1;
            }
        }

        return $"{prefix}-{nextNumber:D3}";
    }

    public async Task<ProductDetailDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        await ValidateCreateAsync(dto, ct);

        var sku = await ResolveSkuAsync(dto.Sku, dto.CategoryId, ct);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            Price = dto.Price,
            PurchaseCost = dto.PurchaseCost,
            PackagingCost = dto.PackagingCost,
            StorageCostDaily = dto.StorageCostDaily,
            Supplier = dto.Supplier,
            Weight = dto.Weight,
            Height = dto.Height,
            Width = dto.Width,
            Length = dto.Length,
            MinStock = dto.MinStock,
            MaxStock = dto.MaxStock,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        await _dispatcher.BroadcastDataChangeAsync("product", "created", product.Id.ToString(), ct);
        await InvalidateListCacheAsync(ct);

        return MapToDetailDto(product, Array.Empty<string>());
    }

    public async Task<ProductDetailDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
            throw new NotFoundException("Produto", id);

        await ValidateUpdateAsync(id, dto, ct);

        var oldValues = new { product.Price, product.PurchaseCost, product.PackagingCost };

        _db.Entry(product).Property(p => p.Version).OriginalValue = dto.Version;

        if (dto.Sku is not null) product.Sku = dto.Sku;
        if (dto.Name is not null) product.Name = dto.Name;
        if (dto.Description is not null) product.Description = dto.Description;
        if (dto.CategoryId is not null) product.CategoryId = dto.CategoryId;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.PurchaseCost.HasValue) product.PurchaseCost = dto.PurchaseCost.Value;
        if (dto.PackagingCost.HasValue) product.PackagingCost = dto.PackagingCost.Value;
        if (dto.StorageCostDaily.HasValue) product.StorageCostDaily = dto.StorageCostDaily.Value == 0 ? null : dto.StorageCostDaily.Value;
        if (dto.Supplier is not null) product.Supplier = dto.Supplier;
        if (dto.Status is not null) product.Status = dto.Status;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
        if (dto.Weight.HasValue) product.Weight = dto.Weight.Value;
        if (dto.Height.HasValue) product.Height = dto.Height.Value;
        if (dto.Width.HasValue) product.Width = dto.Width.Value;
        if (dto.Length.HasValue) product.Length = dto.Length.Value;
        if (dto.MinStock.HasValue) product.MinStock = dto.MinStock.Value == 0 ? null : dto.MinStock.Value;
        if (dto.MaxStock.HasValue) product.MaxStock = dto.MaxStock.Value == 0 ? null : dto.MaxStock.Value;

        product.UpdatedAt = DateTime.UtcNow;
        product.Version++;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException();
        }

        var newValues = new { product.Price, product.PurchaseCost, product.PackagingCost };
        if (oldValues.Price != newValues.Price || oldValues.PurchaseCost != newValues.PurchaseCost || oldValues.PackagingCost != newValues.PackagingCost)
        {
            await _auditService.LogAsync("Atualização de preço/custo", "Product", product.Id, oldValues, newValues, ct);
        }

        await _dispatcher.BroadcastDataChangeAsync("product", "updated", product.Id.ToString(), ct);
        await InvalidateListCacheAsync(ct);

        var photoUrls = await GetPhotoUrlsAsync(id, ct);
        return MapToDetailDto(product, photoUrls);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
            throw new NotFoundException("Produto", id);

        var hasOrders = await _db.OrderItems.AnyAsync(oi => oi.ProductId == id, ct);

        if (hasOrders)
        {
            product.IsActive = false;
            product.Status = "Excluído";
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync(ct);
        }

        await _dispatcher.BroadcastDataChangeAsync("product", "deleted", id.ToString(), ct);
        await InvalidateListCacheAsync(ct);
    }

    // --- Variants ---

    public async Task<IReadOnlyList<ProductVariantDto>> GetVariantsAsync(Guid productId, CancellationToken ct = default)
    {
        await EnsureProductExistsAsync(productId, ct);

        return await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .Select(v => new ProductVariantDto(
                v.Id, v.Sku, v.Attributes, v.Price, v.Stock,
                v.IsActive, v.NeedsReview, v.PurchaseCost,
                v.Weight, v.Height, v.Width, v.Length,
                v.ExternalId, v.PictureIds))
            .ToListAsync(ct);
    }

    public async Task<ProductVariantDto> CreateVariantAsync(Guid productId, CreateProductVariantDto dto, CancellationToken ct = default)
    {
        await EnsureProductExistsAsync(productId, ct);

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = dto.Sku,
            Attributes = dto.Attributes,
            Price = dto.Price,
            Stock = 0,
            IsActive = dto.IsActive,
        };

        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync(ct);

        return MapToVariantDto(variant);
    }

    public async Task<ProductVariantDto> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantDto dto, CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, ct);

        if (variant is null)
            throw new NotFoundException("Variante", variantId);

        if (dto.Sku is not null) variant.Sku = dto.Sku;
        if (dto.Price.HasValue) variant.Price = dto.Price;
        if (dto.IsActive.HasValue) variant.IsActive = dto.IsActive.Value;
        if (dto.PurchaseCost.HasValue) variant.PurchaseCost = dto.PurchaseCost;
        if (dto.Weight.HasValue) variant.Weight = dto.Weight;
        if (dto.Height.HasValue) variant.Height = dto.Height;
        if (dto.Width.HasValue) variant.Width = dto.Width;
        if (dto.Length.HasValue) variant.Length = dto.Length;

        await _db.SaveChangesAsync(ct);
        return MapToVariantDto(variant);
    }

    public async Task DeleteVariantAsync(Guid productId, Guid variantId, CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, ct);

        if (variant is null)
            throw new NotFoundException("Variante", variantId);

        _db.ProductVariants.Remove(variant);
        await _db.SaveChangesAsync(ct);
    }

    // --- Analytics ---

    public async Task<ProductAnalyticsDto> GetAnalyticsAsync(Guid id, int days, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
            throw new NotFoundException("Produto", id);

        var now = DateTime.UtcNow;
        var currentStart = now.AddDays(-days);
        var previousStart = now.AddDays(-days * 2);

        var currentItems = await GetOrderItemsForPeriodAsync(id, currentStart, now, ct);
        var currentSales = currentItems.Sum(x => x.Quantity);
        var currentRevenue = currentItems.Sum(x => x.Subtotal);
        var currentProfit = currentItems.Sum(x => (x.UnitPrice - product.PurchaseCost - product.PackagingCost) * x.Quantity);
        var currentMargin = currentRevenue > 0 ? (currentProfit / currentRevenue) * 100 : (decimal?)null;

        var previousItems = await GetOrderItemsForPeriodAsync(id, previousStart, currentStart, ct);
        var previousSales = previousItems.Sum(x => x.Quantity);
        var previousRevenue = previousItems.Sum(x => x.Subtotal);
        var previousProfit = previousItems.Sum(x => (x.UnitPrice - product.PurchaseCost - product.PackagingCost) * x.Quantity);
        var previousMargin = previousRevenue > 0 ? (previousProfit / previousRevenue) * 100 : (decimal?)null;

        decimal? CalcChange(decimal current, decimal previous) =>
            previous != 0 ? ((current - previous) / Math.Abs(previous)) * 100 : null;

        return new ProductAnalyticsDto(
            currentSales,
            currentRevenue,
            currentProfit,
            currentMargin,
            CalcChange(currentSales, previousSales),
            CalcChange(currentRevenue, previousRevenue),
            CalcChange(currentProfit, previousProfit),
            currentMargin.HasValue && previousMargin.HasValue
                ? currentMargin.Value - previousMargin.Value
                : null);
    }

    public async Task<PagedResult<ProductRecentOrderDto>> GetRecentOrdersAsync(
        Guid id, int days, int page, int pageSize, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
            throw new NotFoundException("Produto", id);

        var cutoff = DateTime.UtcNow.AddDays(-days);

        var query = _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.ProductId == id)
            .Join(_db.Orders.AsNoTracking(),
                oi => oi.OrderId,
                o => o.Id,
                (oi, o) => new { oi, o })
            .Where(x => x.o.CreatedAt >= cutoff);

        var totalCount = await query.CountAsync(ct);

        var rawItems = await query
            .OrderByDescending(x => x.o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                OrderId = x.o.Id,
                Date = x.o.CreatedAt,
                x.oi.Quantity,
                x.oi.UnitPrice,
                x.oi.Subtotal
            })
            .ToListAsync(ct);

        var items = rawItems.Select(x => new ProductRecentOrderDto(
            x.OrderId,
            x.Date,
            x.Quantity,
            x.UnitPrice,
            x.Subtotal,
            (x.UnitPrice - product.PurchaseCost - product.PackagingCost) * x.Quantity))
            .ToList();

        return new PagedResult<ProductRecentOrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // --- Cost History ---

    public async Task<PagedResult<ProductCostHistoryDto>> GetCostHistoryAsync(
        Guid id, int page, int pageSize, CancellationToken ct = default)
    {
        await EnsureProductExistsAsync(id, ct);

        var query = _db.ProductCostHistories
            .AsNoTracking()
            .Where(h => h.ProductId == id);

        var totalCount = await query.CountAsync(ct);

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
            .ToListAsync(ct);

        return new PagedResult<ProductCostHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // --- Private helpers ---

    private async Task<List<Guid>> GetDescendantCategoryIdsAsync(Guid rootId, CancellationToken ct)
    {
        var categoryIds = new List<Guid> { rootId };
        var toCheck = new Queue<Guid>();
        toCheck.Enqueue(rootId);

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

        return categoryIds;
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy, string sortDir)
    {
        bool desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLower() switch
        {
            "sku" => desc ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
            "price" => desc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "status" => desc ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            "createdat" => desc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "stock" => desc
                ? query.OrderByDescending(p => p.Variants.Sum(v => v.Stock))
                : query.OrderBy(p => p.Variants.Sum(v => v.Stock)),
            "margin" => desc
                ? query.OrderByDescending(p => p.Price > 0 ? (p.Price - p.PurchaseCost - p.PackagingCost) / p.Price * 100 : 0)
                : query.OrderBy(p => p.Price > 0 ? (p.Price - p.PurchaseCost - p.PackagingCost) / p.Price * 100 : 0),
            _ => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
        };
    }

    private async Task<string> ResolveSkuAsync(string? requestedSku, string? categoryId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requestedSku))
            return requestedSku;

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            var catId = Guid.TryParse(categoryId, out var cid) ? cid : (Guid?)null;
            if (catId.HasValue)
            {
                var cat = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == catId.Value, ct);
                if (!string.IsNullOrWhiteSpace(cat?.SkuPrefix))
                {
                    var prefix = cat.SkuPrefix;
                    var maxSku = await _db.Products.AsNoTracking()
                        .Where(p => p.Sku.StartsWith(prefix + "-"))
                        .Select(p => p.Sku)
                        .OrderByDescending(s => s)
                        .FirstOrDefaultAsync(ct);
                    var nextNumber = 1;
                    if (maxSku != null)
                    {
                        var suffix = maxSku[(prefix.Length + 1)..];
                        if (int.TryParse(suffix, out var parsed)) nextNumber = parsed + 1;
                    }
                    return $"{prefix}-{nextNumber:D3}";
                }
            }
        }

        return $"PRD-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private async Task ValidateCreateAsync(CreateProductDto dto, CancellationToken ct)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            AddError(errors, "Name", "Nome é obrigatório.");
        else if (dto.Name.Length > 200)
            AddError(errors, "Name", "Nome deve ter no máximo 200 caracteres.");

        if (!string.IsNullOrWhiteSpace(dto.Sku))
        {
            var skuExists = await _db.Products.AsNoTracking()
                .AnyAsync(p => p.Sku == dto.Sku, ct);
            if (skuExists)
                AddError(errors, "Sku", "Este SKU já está em uso por outro produto.");
        }

        if (dto.Price < 0)
            AddError(errors, "Price", "Preço deve ser maior ou igual a zero.");

        if (dto.PurchaseCost < 0)
            AddError(errors, "PurchaseCost", "Custo de compra deve ser maior ou igual a zero.");

        if (dto.PackagingCost < 0)
            AddError(errors, "PackagingCost", "Custo de embalagem deve ser maior ou igual a zero.");

        if (dto.StorageCostDaily.HasValue && dto.StorageCostDaily.Value < 0)
            AddError(errors, "StorageCostDaily", "Custo de armazenagem diário deve ser maior ou igual a zero.");

        if (!string.IsNullOrWhiteSpace(dto.CategoryId))
        {
            if (Guid.TryParse(dto.CategoryId, out var catId))
            {
                var categoryExists = await _db.Categories.AsNoTracking()
                    .AnyAsync(c => c.Id == catId, ct);
                if (!categoryExists)
                    AddError(errors, "CategoryId", "Categoria não encontrada.");
            }
            else
            {
                AddError(errors, "CategoryId", "ID de categoria inválido.");
            }
        }

        if (dto.Weight < 0)
            AddError(errors, "Weight", "Peso deve ser maior ou igual a zero.");
        if (dto.Height < 0)
            AddError(errors, "Height", "Altura deve ser maior ou igual a zero.");
        if (dto.Width < 0)
            AddError(errors, "Width", "Largura deve ser maior ou igual a zero.");
        if (dto.Length < 0)
            AddError(errors, "Length", "Comprimento deve ser maior ou igual a zero.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);
    }

    private async Task ValidateUpdateAsync(Guid id, UpdateProductDto dto, CancellationToken ct)
    {
        var errors = new Dictionary<string, List<string>>();

        if (dto.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                AddError(errors, "Name", "Nome é obrigatório.");
            else if (dto.Name.Length > 200)
                AddError(errors, "Name", "Nome deve ter no máximo 200 caracteres.");
        }

        if (dto.Sku is not null)
        {
            var skuExists = await _db.Products.AsNoTracking()
                .AnyAsync(p => p.Sku == dto.Sku && p.Id != id, ct);
            if (skuExists)
                AddError(errors, "Sku", "Este SKU já está em uso por outro produto.");
        }

        if (dto.Price.HasValue && dto.Price.Value < 0)
            AddError(errors, "Price", "Preço deve ser maior ou igual a zero.");

        if (dto.PurchaseCost.HasValue && dto.PurchaseCost.Value < 0)
            AddError(errors, "PurchaseCost", "Custo de compra deve ser maior ou igual a zero.");

        if (dto.PackagingCost.HasValue && dto.PackagingCost.Value < 0)
            AddError(errors, "PackagingCost", "Custo de embalagem deve ser maior ou igual a zero.");

        if (dto.StorageCostDaily.HasValue && dto.StorageCostDaily.Value < 0)
            AddError(errors, "StorageCostDaily", "Custo de armazenagem diário deve ser maior ou igual a zero.");

        if (dto.CategoryId is not null && !string.IsNullOrWhiteSpace(dto.CategoryId))
        {
            if (Guid.TryParse(dto.CategoryId, out var catId))
            {
                var categoryExists = await _db.Categories.AsNoTracking()
                    .AnyAsync(c => c.Id == catId, ct);
                if (!categoryExists)
                    AddError(errors, "CategoryId", "Categoria não encontrada.");
            }
            else
            {
                AddError(errors, "CategoryId", "ID de categoria inválido.");
            }
        }

        if (dto.Weight.HasValue && dto.Weight.Value < 0)
            AddError(errors, "Weight", "Peso deve ser maior ou igual a zero.");
        if (dto.Height.HasValue && dto.Height.Value < 0)
            AddError(errors, "Height", "Altura deve ser maior ou igual a zero.");
        if (dto.Width.HasValue && dto.Width.Value < 0)
            AddError(errors, "Width", "Largura deve ser maior ou igual a zero.");
        if (dto.Length.HasValue && dto.Length.Value < 0)
            AddError(errors, "Length", "Comprimento deve ser maior ou igual a zero.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);
    }

    private async Task EnsureProductExistsAsync(Guid productId, CancellationToken ct)
    {
        var exists = await _db.Products.AsNoTracking().AnyAsync(p => p.Id == productId, ct);
        if (!exists)
            throw new NotFoundException("Produto", productId);
    }

    private async Task<List<string>> GetPhotoUrlsAsync(Guid productId, CancellationToken ct)
    {
        return await _db.FileUploads
            .AsNoTracking()
            .Where(f => f.EntityType == "product" && f.EntityId == productId && f.IsActive)
            .OrderBy(f => f.SortOrder)
            .Select(f => f.StoragePath)
            .ToListAsync(ct);
    }

    private async Task<List<OrderItemData>> GetOrderItemsForPeriodAsync(
        Guid productId, DateTime from, DateTime to, CancellationToken ct)
    {
        return await _db.OrderItems
            .AsNoTracking()
            .Where(oi => oi.ProductId == productId)
            .Join(_db.Orders.AsNoTracking(),
                oi => oi.OrderId,
                o => o.Id,
                (oi, o) => new { oi, o })
            .Where(x => x.o.CreatedAt >= from && x.o.CreatedAt < to)
            .Select(x => new OrderItemData
            {
                Quantity = x.oi.Quantity,
                UnitPrice = x.oi.UnitPrice,
                Subtotal = x.oi.Subtotal
            })
            .ToListAsync(ct);
    }

    private async Task InvalidateListCacheAsync(CancellationToken ct)
    {
        await _cache.RemoveByPrefixAsync("products:list:", ct);
    }

    private static ProductDetailDto MapToDetailDto(Product product, IReadOnlyList<string> photoUrls)
    {
        return new ProductDetailDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.CategoryId,
            product.Price,
            product.PurchaseCost,
            product.PackagingCost,
            product.StorageCostDaily,
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
            (product.Variants ?? Array.Empty<ProductVariant>()).Select(v => MapToVariantDto(v)).ToList(),
            photoUrls,
            product.Version,
            product.MinStock,
            product.MaxStock);
    }

    private static ProductVariantDto MapToVariantDto(ProductVariant v) => new(
        v.Id, v.Sku, v.Attributes, v.Price, v.Stock,
        v.IsActive, v.NeedsReview, v.PurchaseCost,
        v.Weight, v.Height, v.Width, v.Length,
        v.ExternalId, v.PictureIds);

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = new List<string>();
            errors[field] = list;
        }
        list.Add(message);
    }

    private sealed class OrderItemData
    {
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal Subtotal { get; init; }
    }
}

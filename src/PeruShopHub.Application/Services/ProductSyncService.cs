using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public interface IProductSyncService
{
    Task<ProductSyncResult> SyncConnectionAsync(Guid tenantId, MarketplaceConnection connection, CancellationToken ct = default);
}

public record ProductSyncResult(
    int Checked,
    int Updated,
    int Created,
    int Errors,
    List<string> ErrorMessages);

public class ProductSyncService : IProductSyncService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMlPhotoSyncService _photoSync;
    private readonly ILogger<ProductSyncService> _logger;

    private const int BatchSize = 50;

    public ProductSyncService(
        PeruShopHubDbContext db,
        IServiceProvider serviceProvider,
        IMlPhotoSyncService photoSync,
        ILogger<ProductSyncService> logger)
    {
        _db = db;
        _serviceProvider = serviceProvider;
        _photoSync = photoSync;
        _logger = logger;
    }

    public async Task<ProductSyncResult> SyncConnectionAsync(
        Guid tenantId, MarketplaceConnection connection, CancellationToken ct = default)
    {
        var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>(connection.MarketplaceId);
        if (adapter is null)
        {
            _logger.LogWarning("No adapter registered for marketplace '{Marketplace}'", connection.MarketplaceId);
            return new ProductSyncResult(0, 0, 0, 1, [$"Adaptador '{connection.MarketplaceId}' não disponível."]);
        }

        var tokenEncryption = _serviceProvider.GetRequiredService<ITokenEncryptionService>();
        var accessToken = tokenEncryption.Decrypt(connection.AccessTokenProtected!);

        // Step 1: Scroll through all seller items
        var allItemIds = new List<string>();
        string? scrollId = null;

        do
        {
            ct.ThrowIfCancellationRequested();

            var searchResult = await adapter.SearchSellerItemsAsync(
                connection.ExternalUserId!, scrollId, BatchSize, ct);

            allItemIds.AddRange(searchResult.ItemIds);
            scrollId = searchResult.ScrollId;

            if (searchResult.ItemIds.Count < BatchSize)
                break;

        } while (!string.IsNullOrEmpty(scrollId));

        _logger.LogInformation(
            "Product sync for tenant {TenantId}: found {Count} items to check",
            tenantId, allItemIds.Count);

        // Step 2: Process each item
        int updated = 0, created = 0, errors = 0;
        var errorMessages = new List<string>();

        foreach (var itemId in allItemIds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var wasCreated = await SyncSingleItemAsync(tenantId, adapter, itemId, ct);
                if (wasCreated)
                    created++;
                else
                    updated++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync ML item {ItemId} for tenant {TenantId}", itemId, tenantId);
                errorMessages.Add($"{itemId}: {ex.Message}");
                errors++;
            }
        }

        // Step 3: Detect items that exist locally but are missing from ML (paused/closed)
        await DetectRemovedItemsAsync(tenantId, connection.MarketplaceId, allItemIds, ct);

        // Step 4: Update LastSyncAt
        connection.LastSyncAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new ProductSyncResult(allItemIds.Count, updated, created, errors, errorMessages);
    }

    private async Task<bool> SyncSingleItemAsync(
        Guid tenantId, IMarketplaceAdapter adapter, string externalId, CancellationToken ct)
    {
        var details = await adapter.GetItemDetailsAsync(externalId, ct);

        var existing = await _db.MarketplaceListings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TenantId == tenantId
                && l.MarketplaceId == adapter.MarketplaceId
                && l.ExternalId == externalId, ct);

        var picturesJson = JsonSerializer.Serialize(details.Pictures.Select(p => new { p.Id, p.Url }));
        var variationsJson = details.Variations.Count > 0
            ? JsonSerializer.Serialize(details.Variations.Select(v => new
            {
                v.ExternalVariationId,
                v.Sku,
                v.Price,
                v.AvailableQuantity,
                v.Attributes
            }))
            : null;

        if (existing is not null)
        {
            // Update existing listing
            existing.Title = details.Title;
            existing.Status = details.Status;
            existing.Price = details.Price;
            existing.CategoryId = details.CategoryId;
            existing.Permalink = details.Permalink;
            existing.ThumbnailUrl = details.ThumbnailUrl;
            existing.AvailableQuantity = details.AvailableQuantity;
            existing.PicturesJson = picturesJson;
            existing.VariationsJson = variationsJson;
            existing.FulfillmentType = details.FulfillmentType;
            existing.UpdatedAt = DateTime.UtcNow;

            // Update linked product
            if (existing.ProductId.HasValue)
            {
                await UpdateLinkedProductAsync(tenantId, existing.ProductId.Value, details, ct);
                await SyncPhotosAsync(tenantId, existing.ProductId.Value, details.Pictures, ct);
            }

            await _db.SaveChangesAsync(ct);
            return false; // updated
        }
        else
        {
            // New item discovered on ML — create listing + product
            var product = CreateProductFromListing(tenantId, details);
            _db.Products.Add(product);

            await SyncPhotosAsync(tenantId, product.Id, details.Pictures, ct);

            var listing = new MarketplaceListing
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                MarketplaceId = adapter.MarketplaceId,
                ExternalId = details.ExternalId,
                ProductId = product.Id,
                Title = details.Title,
                Status = details.Status,
                Price = details.Price,
                CategoryId = details.CategoryId,
                Permalink = details.Permalink,
                ThumbnailUrl = details.ThumbnailUrl,
                AvailableQuantity = details.AvailableQuantity,
                PicturesJson = picturesJson,
                VariationsJson = variationsJson,
                FulfillmentType = details.FulfillmentType,
            };

            _db.MarketplaceListings.Add(listing);
            await _db.SaveChangesAsync(ct);
            return true; // created
        }
    }

    private Product CreateProductFromListing(Guid tenantId, MarketplaceItemDetails details)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Sku = $"ML-{details.ExternalId}",
            Name = details.Title,
            Price = details.Price,
            Status = MapMlStatusToInternal(details.Status),
            NeedsReview = true,
            IsActive = details.Status == "active",
        };

        if (details.Variations.Count > 0)
        {
            foreach (var variation in details.Variations)
            {
                var variant = new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = product.Id,
                    Sku = !string.IsNullOrEmpty(variation.Sku)
                        ? variation.Sku
                        : $"ML-{details.ExternalId}-{variation.ExternalVariationId}",
                    Price = variation.Price,
                    Stock = variation.AvailableQuantity,
                    IsActive = true,
                    NeedsReview = true,
                    Attributes = JsonSerializer.Serialize(variation.Attributes),
                    ExternalId = variation.ExternalVariationId,
                    PictureIds = variation.PictureIds is { Count: > 0 }
                        ? JsonSerializer.Serialize(variation.PictureIds)
                        : null,
                };
                _db.ProductVariants.Add(variant);
            }
        }
        else
        {
            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = product.Id,
                Sku = product.Sku,
                Price = details.Price,
                Stock = details.AvailableQuantity,
                IsActive = true,
                IsDefault = true,
                NeedsReview = true,
            };
            _db.ProductVariants.Add(variant);
        }

        return product;
    }

    private async Task UpdateLinkedProductAsync(
        Guid tenantId, Guid productId, MarketplaceItemDetails details, CancellationToken ct)
    {
        var product = await _db.Products
            .IgnoreQueryFilters()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId, ct);

        if (product is null) return;

        product.Name = details.Title;
        product.Price = details.Price;
        product.Status = MapMlStatusToInternal(details.Status);
        product.IsActive = details.Status == "active";
        product.UpdatedAt = DateTime.UtcNow;

        if (details.Variations.Count > 0)
        {
            var matchedVariantIds = new HashSet<Guid>();

            foreach (var variation in details.Variations)
            {
                var sku = !string.IsNullOrEmpty(variation.Sku)
                    ? variation.Sku
                    : $"ML-{details.ExternalId}-{variation.ExternalVariationId}";

                var existingVariant = product.Variants.FirstOrDefault(
                    v => v.ExternalId == variation.ExternalVariationId)
                    ?? product.Variants.FirstOrDefault(v => v.Sku == sku);

                var pictureIds = variation.PictureIds is { Count: > 0 }
                    ? JsonSerializer.Serialize(variation.PictureIds)
                    : null;

                if (existingVariant is not null)
                {
                    existingVariant.Price = variation.Price;
                    existingVariant.Stock = variation.AvailableQuantity;
                    existingVariant.Attributes = JsonSerializer.Serialize(variation.Attributes);
                    existingVariant.ExternalId = variation.ExternalVariationId;
                    existingVariant.PictureIds = pictureIds;
                    existingVariant.IsActive = true;
                    matchedVariantIds.Add(existingVariant.Id);
                }
                else
                {
                    var newVariant = new ProductVariant
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ProductId = productId,
                        Sku = sku,
                        Price = variation.Price,
                        Stock = variation.AvailableQuantity,
                        IsActive = true,
                        NeedsReview = true,
                        Attributes = JsonSerializer.Serialize(variation.Attributes),
                        ExternalId = variation.ExternalVariationId,
                        PictureIds = pictureIds,
                    };
                    _db.ProductVariants.Add(newVariant);
                    matchedVariantIds.Add(newVariant.Id);
                }
            }

            // Mark removed variants as inactive
            foreach (var variant in product.Variants)
            {
                if (!matchedVariantIds.Contains(variant.Id) && variant.ExternalId is not null)
                {
                    variant.IsActive = false;
                }
            }
        }
        else
        {
            var defaultVariant = product.Variants.FirstOrDefault(v => v.IsDefault);
            if (defaultVariant is not null)
            {
                defaultVariant.Price = details.Price;
                defaultVariant.Stock = details.AvailableQuantity;
            }
        }
    }

    /// <summary>
    /// Detects listings that exist locally but were not returned by ML search.
    /// These items were likely paused or closed on ML — update their status.
    /// </summary>
    private async Task DetectRemovedItemsAsync(
        Guid tenantId, string marketplaceId, List<string> activeExternalIds, CancellationToken ct)
    {
        var localListings = await _db.MarketplaceListings
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId
                && l.MarketplaceId == marketplaceId
                && l.Status != "closed")
            .Select(l => new { l.Id, l.ExternalId, l.ProductId })
            .ToListAsync(ct);

        var activeSet = new HashSet<string>(activeExternalIds);
        var missingListings = localListings.Where(l => !activeSet.Contains(l.ExternalId)).ToList();

        if (missingListings.Count == 0) return;

        _logger.LogInformation(
            "Product sync for tenant {TenantId}: {Count} listings not found on ML, marking as closed",
            tenantId, missingListings.Count);

        foreach (var missing in missingListings)
        {
            var listing = await _db.MarketplaceListings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Id == missing.Id, ct);

            if (listing is not null)
            {
                listing.Status = "closed";
                listing.UpdatedAt = DateTime.UtcNow;
            }

            // Update linked product status
            if (missing.ProductId.HasValue)
            {
                var product = await _db.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == missing.ProductId.Value, ct);

                if (product is not null)
                {
                    product.Status = "Inativo";
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }

    private async Task SyncPhotosAsync(
        Guid tenantId, Guid productId,
        IReadOnlyList<MarketplaceItemPicture> pictures,
        CancellationToken ct)
    {
        try
        {
            if (pictures.Count > 0)
                await _photoSync.SyncProductPhotosAsync(tenantId, productId, pictures, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Photo sync failed for product {ProductId}, continuing", productId);
        }
    }

    private static string MapMlStatusToInternal(string mlStatus) => mlStatus switch
    {
        "active" => "Ativo",
        "paused" => "Inativo",
        "closed" => "Inativo",
        "under_review" => "Ativo",
        _ => "Ativo"
    };
}

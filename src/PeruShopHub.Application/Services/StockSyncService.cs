using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public interface IStockSyncService
{
    /// <summary>Enqueue a variant for ML stock sync after an internal stock change.</summary>
    Task EnqueueVariantSyncAsync(Guid tenantId, Guid variantId, CancellationToken ct = default);

    /// <summary>Get the sync status for a specific variant.</summary>
    Task<StockSyncItemStatus?> GetVariantSyncStatusAsync(Guid variantId, CancellationToken ct = default);

    /// <summary>Get sync status for all variants of a product.</summary>
    Task<IReadOnlyList<StockSyncItemStatus>> GetProductSyncStatusesAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Execute pending stock syncs for a given tenant.</summary>
    Task ExecutePendingSyncsAsync(Guid tenantId, CancellationToken ct = default);
}

public record StockSyncItemStatus(
    Guid VariantId,
    string Sku,
    string Status, // Synced, Pending, Error
    int? AllocatedQuantity,
    int? MlQuantity,
    DateTime? LastSyncAt,
    string? ErrorMessage);

public record StockSyncQueueItem(
    Guid TenantId,
    Guid VariantId,
    int Attempt,
    DateTime EnqueuedAt);

public class StockSyncService : IStockSyncService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICacheService _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockSyncService> _logger;

    private const string QueuePrefix = "stocksync:ml:queue:";
    private const string StatusPrefix = "stocksync:ml:status:";
    private const int MaxAttempts = 3;
    private static readonly TimeSpan StatusTtl = TimeSpan.FromHours(24);

    public StockSyncService(
        PeruShopHubDbContext db,
        ICacheService cache,
        IServiceProvider serviceProvider,
        ILogger<StockSyncService> logger)
    {
        _db = db;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task EnqueueVariantSyncAsync(Guid tenantId, Guid variantId, CancellationToken ct = default)
    {
        // Check if this variant is linked to a ML listing
        var hasListing = await _db.Set<MarketplaceListing>()
            .AnyAsync(l => l.TenantId == tenantId
                && l.MarketplaceId == "mercadolivre"
                && l.Product != null
                && l.Product.Variants.Any(v => v.Id == variantId), ct);

        if (!hasListing) return;

        var item = new StockSyncQueueItem(tenantId, variantId, 0, DateTime.UtcNow);
        await _cache.SetAsync($"{QueuePrefix}{tenantId}:{variantId}", item, StatusTtl, ct);

        // Mark variant as pending sync
        await _cache.SetAsync($"{StatusPrefix}{variantId}", new StockSyncItemStatus(
            variantId, "", "Pending", null, null, null, null), StatusTtl, ct);

        _logger.LogInformation("Enqueued ML stock sync for variant {VariantId}, tenant {TenantId}",
            variantId, tenantId);
    }

    public async Task<StockSyncItemStatus?> GetVariantSyncStatusAsync(Guid variantId, CancellationToken ct = default)
    {
        return await _cache.GetAsync<StockSyncItemStatus>($"{StatusPrefix}{variantId}", ct);
    }

    public async Task<IReadOnlyList<StockSyncItemStatus>> GetProductSyncStatusesAsync(Guid productId, CancellationToken ct = default)
    {
        var variants = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.ProductId == productId)
            .Select(v => new { v.Id, v.Sku })
            .ToListAsync(ct);

        var statuses = new List<StockSyncItemStatus>();
        foreach (var v in variants)
        {
            var status = await _cache.GetAsync<StockSyncItemStatus>($"{StatusPrefix}{v.Id}", ct);
            if (status is not null)
            {
                statuses.Add(status with { Sku = v.Sku });
            }
        }

        return statuses;
    }

    public async Task ExecutePendingSyncsAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Find all variants with ML listings for this tenant
        var linkedVariants = await _db.Set<MarketplaceListing>()
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId
                && l.MarketplaceId == "mercadolivre"
                && l.ProductId != null)
            .Select(l => new
            {
                l.ExternalId,
                l.ProductId,
                Variants = l.Product!.Variants.Select(v => new
                {
                    v.Id,
                    v.Sku,
                    v.ExternalId,
                    v.Stock
                }).ToList()
            })
            .ToListAsync(ct);

        // Get allocations for all relevant variant IDs
        var allVariantIds = linkedVariants.SelectMany(l => l.Variants.Select(v => v.Id)).Distinct().ToList();

        // Check which variants have pending sync
        var pendingVariantIds = new List<Guid>();
        foreach (var vid in allVariantIds)
        {
            var queueItem = await _cache.GetAsync<StockSyncQueueItem>($"{QueuePrefix}{tenantId}:{vid}", ct);
            if (queueItem is not null)
                pendingVariantIds.Add(vid);
        }

        if (pendingVariantIds.Count == 0) return;

        // Get allocations for pending variants
        var allocations = await _db.StockAllocations
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId
                && a.MarketplaceId == "mercadolivre"
                && pendingVariantIds.Contains(a.ProductVariantId))
            .ToDictionaryAsync(a => a.ProductVariantId, a => a.AllocatedQuantity - a.ReservedQuantity, ct);

        var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre");
        if (adapter is null)
        {
            _logger.LogWarning("ML adapter not available for stock sync");
            return;
        }

        foreach (var listing in linkedVariants)
        {
            foreach (var variant in listing.Variants)
            {
                if (!pendingVariantIds.Contains(variant.Id)) continue;

                var queueItem = await _cache.GetAsync<StockSyncQueueItem>($"{QueuePrefix}{tenantId}:{variant.Id}", ct);
                if (queueItem is null) continue;

                // Use allocated quantity (minus reserved), falling back to variant stock
                var quantity = allocations.TryGetValue(variant.Id, out var allocated)
                    ? Math.Max(0, allocated)
                    : variant.Stock;

                try
                {
                    _logger.LogInformation(
                        "Syncing stock to ML: variant={VariantId}, sku={Sku}, item={ItemId}, variation={VariationId}, qty={Qty}, attempt={Attempt}",
                        variant.Id, variant.Sku, listing.ExternalId, variant.ExternalId, quantity, queueItem.Attempt + 1);

                    if (!string.IsNullOrWhiteSpace(variant.ExternalId))
                    {
                        // Variation-level update: PUT /items/{itemId}/variations/{variationId}
                        await adapter.UpdateVariationStockAsync(
                            listing.ExternalId, variant.ExternalId, quantity, ct);
                    }
                    else
                    {
                        // Item-level update: PUT /items/{itemId}
                        await adapter.UpdateStockAsync(listing.ExternalId, quantity, ct);
                    }

                    // Update MarketplaceListing.AvailableQuantity
                    var listingEntity = await _db.Set<MarketplaceListing>()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.ExternalId == listing.ExternalId, ct);

                    if (listingEntity is not null)
                    {
                        listingEntity.AvailableQuantity = quantity;
                        listingEntity.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync(ct);
                    }

                    // Mark as synced
                    await _cache.SetAsync($"{StatusPrefix}{variant.Id}", new StockSyncItemStatus(
                        variant.Id, variant.Sku, "Synced", quantity, quantity, DateTime.UtcNow, null), StatusTtl, ct);

                    // Remove from queue
                    await _cache.RemoveAsync($"{QueuePrefix}{tenantId}:{variant.Id}", ct);

                    _logger.LogInformation("ML stock sync succeeded for variant {VariantId}, qty={Qty}",
                        variant.Id, quantity);
                }
                catch (Exception ex)
                {
                    var attempt = queueItem.Attempt + 1;
                    _logger.LogWarning(ex,
                        "ML stock sync failed for variant {VariantId}, attempt {Attempt}/{MaxAttempts}",
                        variant.Id, attempt, MaxAttempts);

                    if (attempt >= MaxAttempts)
                    {
                        // Max retries reached — mark as error
                        await _cache.SetAsync($"{StatusPrefix}{variant.Id}", new StockSyncItemStatus(
                            variant.Id, variant.Sku, "Error", quantity, null, DateTime.UtcNow, ex.Message), StatusTtl, ct);
                        await _cache.RemoveAsync($"{QueuePrefix}{tenantId}:{variant.Id}", ct);

                        _logger.LogError("ML stock sync permanently failed for variant {VariantId} after {MaxAttempts} attempts",
                            variant.Id, MaxAttempts);
                    }
                    else
                    {
                        // Re-enqueue with incremented attempt
                        await _cache.SetAsync($"{QueuePrefix}{tenantId}:{variant.Id}",
                            queueItem with { Attempt = attempt }, StatusTtl, ct);
                    }
                }
            }
        }
    }
}

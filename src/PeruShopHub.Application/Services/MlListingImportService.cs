using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public interface IMlListingImportService
{
    Task<ImportJobStatus> EnqueueImportAsync(Guid tenantId, CancellationToken ct = default);
    Task<ImportJobStatus?> GetImportStatusAsync(Guid tenantId, CancellationToken ct = default);
    Task ExecuteImportAsync(Guid tenantId, CancellationToken ct = default);
}

public record ImportJobStatus(
    string Status, // Queued, Running, Completed, Failed
    int TotalItems,
    int ProcessedItems,
    int ErrorCount,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    List<string>? Errors);

public class MlListingImportService : IMlListingImportService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ICacheService _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MlListingImportService> _logger;

    private const string ImportStatusPrefix = "import:ml:";
    private const string ImportQueueKey = "import:ml:queue";
    private const int BatchSize = 50;
    private static readonly TimeSpan StatusTtl = TimeSpan.FromHours(24);

    public MlListingImportService(
        PeruShopHubDbContext db,
        ICacheService cache,
        IServiceProvider serviceProvider,
        ILogger<MlListingImportService> logger)
    {
        _db = db;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<ImportJobStatus> EnqueueImportAsync(Guid tenantId, CancellationToken ct = default)
    {
        // Check if already running
        var existing = await GetImportStatusAsync(tenantId, ct);
        if (existing is { Status: "Queued" or "Running" })
            return existing;

        // Verify ML connection is active
        var connection = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(c => c.MarketplaceId == "mercadolivre" && c.IsConnected && c.Status == "Active", ct)
            ?? throw new InvalidOperationException("Mercado Livre não está conectado ou não está ativo.");

        var status = new ImportJobStatus("Queued", 0, 0, 0, null, null, null);
        await SetStatusAsync(tenantId, status, ct);

        // Enqueue tenant for import
        await _cache.SetAsync($"{ImportQueueKey}:{tenantId}", tenantId.ToString(), StatusTtl, ct);

        _logger.LogInformation("ML listing import enqueued for tenant {TenantId}", tenantId);
        return status;
    }

    public async Task<ImportJobStatus?> GetImportStatusAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _cache.GetAsync<ImportJobStatus>($"{ImportStatusPrefix}{tenantId}", ct);
    }

    public async Task ExecuteImportAsync(Guid tenantId, CancellationToken ct = default)
    {
        var errors = new List<string>();

        try
        {
            // Get connection (ignore query filters since we're in worker context)
            var connection = await _db.MarketplaceConnections
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.TenantId == tenantId
                    && c.MarketplaceId == "mercadolivre"
                    && c.IsConnected, ct);

            if (connection is null)
            {
                await SetStatusAsync(tenantId, new ImportJobStatus("Failed", 0, 0, 1, DateTime.UtcNow, DateTime.UtcNow,
                    ["Conexão com Mercado Livre não encontrada."]), ct);
                return;
            }

            var adapter = _serviceProvider.GetKeyedService<IMarketplaceAdapter>("mercadolivre");
            if (adapter is null)
            {
                await SetStatusAsync(tenantId, new ImportJobStatus("Failed", 0, 0, 1, DateTime.UtcNow, DateTime.UtcNow,
                    ["Adaptador do Mercado Livre não disponível."]), ct);
                return;
            }

            var tokenEncryption = _serviceProvider.GetRequiredService<ITokenEncryptionService>();
            var accessToken = tokenEncryption.Decrypt(connection.AccessTokenProtected!);

            var startedAt = DateTime.UtcNow;
            await SetStatusAsync(tenantId, new ImportJobStatus("Running", 0, 0, 0, startedAt, null, null), ct);

            // Step 1: Scroll through all items to collect IDs
            var allItemIds = new List<string>();
            string? scrollId = null;
            int total = 0;

            do
            {
                ct.ThrowIfCancellationRequested();

                var searchResult = await adapter.SearchSellerItemsAsync(
                    connection.ExternalUserId!, scrollId, BatchSize, ct);

                allItemIds.AddRange(searchResult.ItemIds);
                scrollId = searchResult.ScrollId;
                total = searchResult.Total;

                await SetStatusAsync(tenantId, new ImportJobStatus("Running", total, 0, 0, startedAt, null, null), ct);

                // Break if we got fewer results than limit (end of scroll)
                if (searchResult.ItemIds.Count < BatchSize)
                    break;

            } while (!string.IsNullOrEmpty(scrollId));

            _logger.LogInformation("ML import for tenant {TenantId}: found {Count} items (total reported: {Total})",
                tenantId, allItemIds.Count, total);

            // Step 2: Process items in batches
            int processed = 0;
            var batches = allItemIds.Chunk(BatchSize);

            foreach (var batch in batches)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var itemId in batch)
                {
                    try
                    {
                        await ImportSingleItemAsync(tenantId, adapter, itemId, ct);
                        processed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to import ML item {ItemId} for tenant {TenantId}", itemId, tenantId);
                        errors.Add($"{itemId}: {ex.Message}");
                    }

                    // Update progress
                    await SetStatusAsync(tenantId, new ImportJobStatus("Running", total, processed, errors.Count, startedAt, null,
                        errors.Count > 0 ? errors.Take(50).ToList() : null), ct);
                }
            }

            // Update connection last sync
            connection.LastSyncAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            var finalStatus = new ImportJobStatus("Completed", total, processed, errors.Count,
                startedAt, DateTime.UtcNow, errors.Count > 0 ? errors.Take(50).ToList() : null);
            await SetStatusAsync(tenantId, finalStatus, ct);

            _logger.LogInformation(
                "ML import completed for tenant {TenantId}: {Processed}/{Total} items, {Errors} errors",
                tenantId, processed, total, errors.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ML import cancelled for tenant {TenantId}", tenantId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ML import failed for tenant {TenantId}", tenantId);
            errors.Add($"Erro geral: {ex.Message}");
            await SetStatusAsync(tenantId, new ImportJobStatus("Failed", 0, 0, errors.Count,
                null, DateTime.UtcNow, errors.Take(50).ToList()), ct);
        }
        finally
        {
            await _cache.RemoveAsync($"{ImportQueueKey}:{tenantId}", ct);
        }
    }

    private async Task ImportSingleItemAsync(
        Guid tenantId, IMarketplaceAdapter adapter, string externalId, CancellationToken ct)
    {
        var details = await adapter.GetItemDetailsAsync(externalId, ct);

        // Check if listing already exists
        var existing = await _db.MarketplaceListings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TenantId == tenantId
                && l.MarketplaceId == "mercadolivre"
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
            existing.UpdatedAt = DateTime.UtcNow;

            // Update linked product if exists
            if (existing.ProductId.HasValue)
            {
                await UpdateLinkedProductAsync(tenantId, existing.ProductId.Value, details, ct);
            }
        }
        else
        {
            // Create new listing + product
            var product = await CreateProductFromListingAsync(tenantId, details, ct);

            var listing = new MarketplaceListing
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                MarketplaceId = "mercadolivre",
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
            };

            _db.MarketplaceListings.Add(listing);
        }

        await _db.SaveChangesAsync(ct);
    }

    private Task<Product> CreateProductFromListingAsync(
        Guid tenantId, MarketplaceItemDetails details, CancellationToken ct)
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

        _db.Products.Add(product);

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
                };
                _db.ProductVariants.Add(variant);
            }
        }
        else
        {
            // Create a default variant
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

        return Task.FromResult(product);
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
        product.IsActive = details.Status == "active";
        product.UpdatedAt = DateTime.UtcNow;

        // Update variants if listing has variations
        if (details.Variations.Count > 0)
        {
            foreach (var variation in details.Variations)
            {
                var sku = !string.IsNullOrEmpty(variation.Sku)
                    ? variation.Sku
                    : $"ML-{details.ExternalId}-{variation.ExternalVariationId}";

                var existingVariant = product.Variants.FirstOrDefault(v => v.Sku == sku);
                if (existingVariant is not null)
                {
                    existingVariant.Price = variation.Price;
                    existingVariant.Stock = variation.AvailableQuantity;
                    existingVariant.Attributes = JsonSerializer.Serialize(variation.Attributes);
                }
                else
                {
                    _db.ProductVariants.Add(new ProductVariant
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
                    });
                }
            }
        }
        else
        {
            // Update default variant
            var defaultVariant = product.Variants.FirstOrDefault(v => v.IsDefault);
            if (defaultVariant is not null)
            {
                defaultVariant.Price = details.Price;
                defaultVariant.Stock = details.AvailableQuantity;
            }
        }
    }

    private async Task SetStatusAsync(Guid tenantId, ImportJobStatus status, CancellationToken ct)
    {
        await _cache.SetAsync($"{ImportStatusPrefix}{tenantId}", status, StatusTtl, ct);
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

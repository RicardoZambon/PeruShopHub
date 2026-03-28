using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public interface IMlPhotoSyncService
{
    Task SyncProductPhotosAsync(
        Guid tenantId, Guid productId,
        IReadOnlyList<MarketplaceItemPicture> pictures,
        CancellationToken ct = default);
}

public class MlPhotoSyncService : IMlPhotoSyncService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MlPhotoSyncService> _logger;

    private const string EntityType = "product";
    private const string Folder = "product";

    public MlPhotoSyncService(
        PeruShopHubDbContext db,
        IFileStorageService storage,
        IHttpClientFactory httpClientFactory,
        ILogger<MlPhotoSyncService> logger)
    {
        _db = db;
        _storage = storage;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SyncProductPhotosAsync(
        Guid tenantId, Guid productId,
        IReadOnlyList<MarketplaceItemPicture> pictures,
        CancellationToken ct = default)
    {
        // Get existing photo uploads for this product
        var existingUploads = await _db.FileUploads
            .IgnoreQueryFilters()
            .Where(f => f.TenantId == tenantId
                && f.EntityType == EntityType
                && f.EntityId == productId)
            .ToListAsync(ct);

        var existingByUrl = existingUploads
            .Where(f => f.ExternalUrl is not null)
            .ToDictionary(f => f.ExternalUrl!, f => f);

        var incomingUrls = new HashSet<string>();

        for (var i = 0; i < pictures.Count; i++)
        {
            var pic = pictures[i];
            var url = pic.Url;
            incomingUrls.Add(url);

            if (existingByUrl.TryGetValue(url, out var existing))
            {
                // Photo already exists — ensure active and update sort order
                existing.IsActive = true;
                existing.SortOrder = i;
            }
            else
            {
                // New photo — download and store
                await DownloadAndStoreAsync(tenantId, productId, url, i, ct);
            }
        }

        // Mark photos no longer in ML as inactive
        foreach (var upload in existingUploads)
        {
            if (upload.ExternalUrl is not null && !incomingUrls.Contains(upload.ExternalUrl))
            {
                upload.IsActive = false;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task DownloadAndStoreAsync(
        Guid tenantId, Guid productId, string url, int sortOrder, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("MercadoLivre");
            using var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download ML photo {Url}: {StatusCode}", url, response.StatusCode);
                return;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var extension = contentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
            var fileName = $"ml-{Guid.NewGuid():N}{extension}";

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var sizeBytes = response.Content.Headers.ContentLength ?? stream.Length;

            var storagePath = await _storage.UploadAsync(stream, fileName, contentType, Folder, ct);

            var upload = new FileUpload
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EntityType = EntityType,
                EntityId = productId,
                FileName = fileName,
                StoragePath = storagePath,
                ContentType = contentType,
                SizeBytes = sizeBytes,
                SortOrder = sortOrder,
                ExternalUrl = url,
                IsActive = true
            };

            _db.FileUploads.Add(upload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Error downloading ML photo {Url} for product {ProductId}", url, productId);
        }
    }
}

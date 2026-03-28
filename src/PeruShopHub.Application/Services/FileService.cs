using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Files;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class FileService : IFileService
{
    private readonly PeruShopHubDbContext _db;
    private readonly IFileStorageService _storage;

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/jpg", "image/png", "image/webp" };

    private const long MaxFileSize = 5 * 1024 * 1024;

    public FileService(PeruShopHubDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<FileUploadDto> UploadAsync(
        Stream fileStream, string fileName, string contentType, long sizeBytes,
        string entityType, Guid entityId, int sortOrder,
        CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>();

        if (sizeBytes == 0)
            errors["File"] = ["Arquivo vazio."];
        else if (sizeBytes > MaxFileSize)
            errors["File"] = ["Arquivo excede o limite de 5MB."];

        if (!AllowedTypes.Contains(contentType))
            (errors.TryGetValue("File", out var list) ? list : (errors["File"] = [])).Add("Tipo de arquivo não permitido.");

        if (errors.Count > 0)
            throw new AppValidationException(errors);

        var storagePath = await _storage.UploadAsync(fileStream, fileName, contentType, entityType, ct);

        var upload = new FileUpload
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            SortOrder = sortOrder
        };

        _db.FileUploads.Add(upload);
        await _db.SaveChangesAsync(ct);

        return new FileUploadDto(
            upload.Id, _storage.GetPublicUrl(upload.StoragePath),
            upload.FileName, upload.ContentType, upload.SizeBytes, upload.SortOrder);
    }

    public async Task<IReadOnlyList<FileUploadDto>> GetByEntityAsync(
        string entityType, Guid entityId,
        CancellationToken ct = default)
    {
        return await _db.FileUploads
            .Where(f => f.EntityType == entityType && f.EntityId == entityId && f.IsActive)
            .OrderBy(f => f.SortOrder)
            .Select(f => new FileUploadDto(
                f.Id, _storage.GetPublicUrl(f.StoragePath),
                f.FileName, f.ContentType, f.SizeBytes, f.SortOrder))
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var file = await _db.FileUploads.FindAsync([id], ct)
            ?? throw new NotFoundException("Arquivo", id);

        await _storage.DeleteAsync(file.StoragePath, ct);
        _db.FileUploads.Remove(file);
        await _db.SaveChangesAsync(ct);
    }
}

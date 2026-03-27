using PeruShopHub.Application.DTOs.Files;

namespace PeruShopHub.Application.Services;

public interface IFileService
{
    Task<FileUploadDto> UploadAsync(
        Stream fileStream, string fileName, string contentType, long sizeBytes,
        string entityType, Guid entityId, int sortOrder,
        CancellationToken ct = default);

    Task<IReadOnlyList<FileUploadDto>> GetByEntityAsync(
        string entityType, Guid entityId,
        CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

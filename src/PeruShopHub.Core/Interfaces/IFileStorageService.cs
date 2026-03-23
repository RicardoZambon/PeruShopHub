namespace PeruShopHub.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream file, string fileName, string contentType, string folder, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
    string GetPublicUrl(string storagePath);
}

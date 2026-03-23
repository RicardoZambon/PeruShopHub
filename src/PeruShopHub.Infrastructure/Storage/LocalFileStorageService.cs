using Microsoft.Extensions.Configuration;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration config)
    {
        _basePath = config["FileStorage:BasePath"] ?? "wwwroot/uploads";
    }

    public async Task<string> UploadAsync(Stream file, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var safeName = $"{Guid.NewGuid():N}-{SanitizeFileName(fileName)}";
        var relativePath = Path.Combine(folder, safeName);
        var fullPath = Path.Combine(_basePath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(fs, ct);
        return relativePath.Replace('\\', '/');
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storagePath) => $"/uploads/{storagePath}";

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}

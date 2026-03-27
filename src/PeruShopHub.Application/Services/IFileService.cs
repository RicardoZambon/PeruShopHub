namespace PeruShopHub.Application.Services;

public interface IFileService
{
    Task<object> UploadAsync(object dto, CancellationToken ct = default);
    Task<object> GetByEntityAsync(string entityType, int entityId, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

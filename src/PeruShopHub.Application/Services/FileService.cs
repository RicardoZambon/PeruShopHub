using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class FileService : IFileService
{
    private readonly PeruShopHubDbContext _db;

    public FileService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public Task<object> UploadAsync(object dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object> GetByEntityAsync(string entityType, int entityId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => throw new NotImplementedException();
}

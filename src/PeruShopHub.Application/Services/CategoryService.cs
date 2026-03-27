using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly PeruShopHubDbContext _db;

    public CategoryService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public Task<object?> GetByParentAsync(int? parentId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object?> GetTreeAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object> CreateAsync(object dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object> UpdateAsync(int id, object dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => throw new NotImplementedException();
}

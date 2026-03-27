namespace PeruShopHub.Application.Services;

public interface ICategoryService
{
    Task<object?> GetByParentAsync(int? parentId, CancellationToken ct = default);
    Task<object?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<object?> GetTreeAsync(CancellationToken ct = default);
    Task<object> CreateAsync(object dto, CancellationToken ct = default);
    Task<object> UpdateAsync(int id, object dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

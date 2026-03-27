namespace PeruShopHub.Application.Services;

public interface IProductService
{
    Task<object> GetListAsync(CancellationToken ct = default);
    Task<object?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<object> CreateAsync(object dto, CancellationToken ct = default);
    Task<object> UpdateAsync(int id, object dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

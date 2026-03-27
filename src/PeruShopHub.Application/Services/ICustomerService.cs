namespace PeruShopHub.Application.Services;

public interface ICustomerService
{
    Task<object> GetListAsync(CancellationToken ct = default);
    Task<object?> GetByIdAsync(int id, CancellationToken ct = default);
}

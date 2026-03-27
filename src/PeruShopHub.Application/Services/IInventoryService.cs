namespace PeruShopHub.Application.Services;

public interface IInventoryService
{
    Task<object> GetOverviewAsync(CancellationToken ct = default);
    Task<object> GetMovementsAsync(CancellationToken ct = default);
}

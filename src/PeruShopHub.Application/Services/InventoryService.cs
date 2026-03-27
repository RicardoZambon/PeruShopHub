using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly PeruShopHubDbContext _db;

    public InventoryService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public Task<object> GetOverviewAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object> GetMovementsAsync(CancellationToken ct = default)
        => throw new NotImplementedException();
}

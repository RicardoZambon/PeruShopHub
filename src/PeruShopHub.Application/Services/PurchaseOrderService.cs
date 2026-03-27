using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly PeruShopHubDbContext _db;

    public PurchaseOrderService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public Task<object> GetListAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object> CreateAsync(object dto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object> UpdateAsync(int id, object dto, CancellationToken ct = default)
        => throw new NotImplementedException();
}

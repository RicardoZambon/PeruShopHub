using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class OrderService : IOrderService
{
    private readonly PeruShopHubDbContext _db;

    public OrderService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public Task<object> GetListAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new NotImplementedException();
}

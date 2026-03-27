using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class UserService : IUserService
{
    private readonly PeruShopHubDbContext _db;

    public UserService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public Task<object> GetListAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<object?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new NotImplementedException();
}

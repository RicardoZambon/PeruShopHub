using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly PeruShopHubDbContext _db;

    public DashboardService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public Task<object> GetSummaryAsync(CancellationToken ct = default)
        => throw new NotImplementedException();
}

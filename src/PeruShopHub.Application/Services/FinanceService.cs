using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class FinanceService : IFinanceService
{
    private readonly PeruShopHubDbContext _db;

    public FinanceService(PeruShopHubDbContext db)
    {
        _db = db;
    }

    public Task<object> GetSummaryAsync(CancellationToken ct = default)
        => throw new NotImplementedException();
}

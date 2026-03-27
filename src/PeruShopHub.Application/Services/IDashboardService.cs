namespace PeruShopHub.Application.Services;

public interface IDashboardService
{
    Task<object> GetSummaryAsync(CancellationToken ct = default);
}

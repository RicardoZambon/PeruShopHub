namespace PeruShopHub.Application.Services;

public interface IFinanceService
{
    Task<object> GetSummaryAsync(CancellationToken ct = default);
}

using PeruShopHub.Application.DTOs.Dashboard;
using PeruShopHub.Application.DTOs.Finance;

namespace PeruShopHub.Application.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(string period, CancellationToken ct = default);
    Task<IReadOnlyList<ChartDataPointDto>> GetRevenueProfitChartAsync(int days, CancellationToken ct = default);
    Task<IReadOnlyList<CostBreakdownDto>> GetCostBreakdownAsync(string period, CancellationToken ct = default);
    Task<IReadOnlyList<ProductRankingDto>> GetTopProductsAsync(int limit, CancellationToken ct = default);
    Task<IReadOnlyList<ProductRankingDto>> GetLeastProfitableAsync(int limit, CancellationToken ct = default);
    Task<IReadOnlyList<PendingActionDto>> GetPendingActionsAsync(CancellationToken ct = default);
}

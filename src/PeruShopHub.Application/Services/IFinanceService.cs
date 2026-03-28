using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Dashboard;
using PeruShopHub.Application.DTOs.Finance;

namespace PeruShopHub.Application.Services;

public interface IFinanceService
{
    Task<FinanceSummaryDto> GetSummaryAsync(string period, CancellationToken ct = default);
    Task<IReadOnlyList<ChartDataPointDto>> GetRevenueProfitChartAsync(int days, CancellationToken ct = default);
    Task<IReadOnlyList<MarginChartPointDto>> GetMarginChartAsync(int days, CancellationToken ct = default);
    Task<PagedResult<SkuProfitabilityDetailDto>> GetSkuProfitabilityAsync(
        int page, int pageSize, string sortBy, string sortDir,
        string? search = null, decimal? minMargin = null, decimal? maxMargin = null,
        DateTime? dateFrom = null, DateTime? dateTo = null,
        CancellationToken ct = default);
    Task RefreshSkuProfitabilityAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyReconciliationDto>> GetReconciliationAsync(int year, CancellationToken ct = default);
    Task<IReadOnlyList<AbcProductDto>> GetAbcCurveAsync(DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
}

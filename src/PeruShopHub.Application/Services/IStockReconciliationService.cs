using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Inventory;

namespace PeruShopHub.Application.Services;

public interface IStockReconciliationService
{
    /// <summary>
    /// Runs a full reconciliation cycle for a tenant, comparing local stock with ML stock.
    /// Called by StockReconciliationWorker.
    /// </summary>
    Task<Guid> RunReconciliationAsync(Guid tenantId, int autoCorrectThreshold, CancellationToken ct = default);

    /// <summary>
    /// Get paginated reconciliation reports for the current tenant.
    /// </summary>
    Task<PagedResult<ReconciliationReportDto>> GetReportsAsync(
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Get a single reconciliation report with all items.
    /// </summary>
    Task<ReconciliationReportDetailDto> GetReportDetailAsync(Guid reportId, CancellationToken ct = default);
}

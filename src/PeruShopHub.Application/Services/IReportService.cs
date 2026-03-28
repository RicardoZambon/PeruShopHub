namespace PeruShopHub.Application.Services;

public interface IReportService
{
    Task<byte[]> GenerateProfitabilityReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<byte[]> GenerateOrderReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<byte[]> GenerateInventoryReportAsync(CancellationToken ct = default);
}

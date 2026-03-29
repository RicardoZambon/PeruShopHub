namespace PeruShopHub.Application.Services;

public interface IReportService
{
    Task<byte[]> GenerateProfitabilityReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<byte[]> GenerateOrderReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<byte[]> GenerateInventoryReportAsync(CancellationToken ct = default);

    Task<byte[]> ExportProfitabilityToExcelAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<byte[]> ExportOrdersToExcelAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<byte[]> ExportInventoryToExcelAsync(CancellationToken ct = default);

    Task<byte[]> ExportAccountingAsync(string format, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
}

using PeruShopHub.Application.DTOs.Profile;

namespace PeruShopHub.Application.Services;

public interface IUserDataExportService
{
    Task<UserDataExportDto> RequestExportAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task<UserDataExportDto?> GetExportStatusAsync(Guid exportId, Guid userId, CancellationToken ct = default);
    Task<(byte[] Data, string FileName)?> DownloadExportAsync(Guid exportId, Guid userId, CancellationToken ct = default);
    Task ProcessPendingExportsAsync(CancellationToken ct = default);
}

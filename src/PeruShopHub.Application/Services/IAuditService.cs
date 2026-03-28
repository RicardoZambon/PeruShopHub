using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.AuditLogs;

namespace PeruShopHub.Application.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, Guid? entityId, object? oldValue, object? newValue, CancellationToken ct = default);

    Task<PagedResult<AuditLogDto>> GetListAsync(
        int page, int pageSize,
        string? entityType, Guid? entityId,
        DateTime? dateFrom, DateTime? dateTo,
        Guid? userId,
        CancellationToken ct = default);
}

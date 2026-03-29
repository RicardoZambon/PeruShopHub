using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Claims;

namespace PeruShopHub.Application.Services;

public interface IClaimService
{
    Task<PagedResult<ClaimListDto>> GetListAsync(
        string? status, string? type, int page = 1, int pageSize = 20, CancellationToken ct = default);

    Task<ClaimDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ClaimDetailDto> RespondAsync(Guid id, RespondClaimRequest request, CancellationToken ct = default);

    Task<ClaimSummaryDto> GetSummaryAsync(CancellationToken ct = default);

    Task SyncClaimsAsync(Guid tenantId, CancellationToken ct = default);

    Task SyncSingleClaimAsync(string externalClaimId, Guid tenantId, CancellationToken ct = default);
}

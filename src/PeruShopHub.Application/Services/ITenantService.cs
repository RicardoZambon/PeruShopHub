using PeruShopHub.Application.DTOs.Tenant;

namespace PeruShopHub.Application.Services;

public interface ITenantService
{
    Task<TenantDetailDto> GetByIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantDetailDto> UpdateAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TenantDetailDto>> GetAllAsync(CancellationToken ct = default);
    Task SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct = default);
}

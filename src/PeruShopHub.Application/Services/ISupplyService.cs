using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Supplies;

namespace PeruShopHub.Application.Services;

public interface ISupplyService
{
    Task<PagedResult<SupplyListDto>> GetListAsync(
        int page, int pageSize, string? search,
        string? category, string? status,
        string sortBy, string sortDir,
        CancellationToken ct = default);

    Task<SupplyDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<SupplyListDto> CreateAsync(CreateSupplyDto dto, CancellationToken ct = default);

    Task<SupplyListDto> UpdateAsync(Guid id, UpdateSupplyDto dto, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

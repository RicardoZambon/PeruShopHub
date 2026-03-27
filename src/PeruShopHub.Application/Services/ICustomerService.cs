using PeruShopHub.Application.Common;
using PeruShopHub.Application.DTOs.Customers;

namespace PeruShopHub.Application.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerListDto>> GetListAsync(
        int page, int pageSize, string? search,
        string sortBy, string sortDir,
        CancellationToken ct = default);

    Task<CustomerDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
}

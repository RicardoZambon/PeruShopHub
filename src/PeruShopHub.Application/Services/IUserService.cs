using PeruShopHub.Application.DTOs.Settings;

namespace PeruShopHub.Application.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDetailDto>> GetListAsync(CancellationToken ct = default);
    Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDetailDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}

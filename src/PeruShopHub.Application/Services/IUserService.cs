using PeruShopHub.Application.DTOs.Settings;

namespace PeruShopHub.Application.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDetailDto>> GetTenantMembersAsync(Guid tenantId, CancellationToken ct = default);
    Task<UserDetailDto> InviteMemberAsync(Guid tenantId, CreateUserRequest request, CancellationToken ct = default);
    Task<UserDetailDto> UpdateMemberAsync(Guid tenantId, Guid userId, UpdateUserRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}

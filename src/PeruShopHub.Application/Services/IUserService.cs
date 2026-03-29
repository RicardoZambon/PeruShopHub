using PeruShopHub.Application.DTOs.Profile;
using PeruShopHub.Application.DTOs.Settings;

namespace PeruShopHub.Application.Services;

public interface IUserService
{
    Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<ProfileDto> UpdateProfileEmailAsync(Guid userId, UpdateProfileEmailRequest request, CancellationToken ct = default);
    Task<ProfileDto> UpdateProfileAvatarAsync(Guid userId, string avatarUrl, CancellationToken ct = default);
    Task RemoveProfileAvatarAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserDetailDto>> GetTenantMembersAsync(Guid tenantId, CancellationToken ct = default);
    Task<UserDetailDto> InviteMemberAsync(Guid tenantId, CreateUserRequest request, CancellationToken ct = default);
    Task<UserDetailDto> UpdateMemberAsync(Guid tenantId, Guid userId, UpdateUserRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<AccountDeletionDto> RequestAccountDeletionAsync(Guid userId, DeleteAccountRequest request, CancellationToken ct = default);
    Task CancelAccountDeletionAsync(Guid userId, CancellationToken ct = default);
    Task<AccountDeletionDto?> GetPendingDeletionAsync(Guid userId, CancellationToken ct = default);
    Task ProcessExpiredDeletionsAsync(CancellationToken ct = default);
}

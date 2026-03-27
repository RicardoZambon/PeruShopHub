namespace PeruShopHub.Application.DTOs.Tenant;

public record TenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    int MemberCount,
    DateTime CreatedAt);

public record UpdateTenantRequest(string Name);

public record InviteMemberRequest(string Email, string Role);

public record UpdateMemberRoleRequest(string Role);

namespace PeruShopHub.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string ShopName, string Name, string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record SwitchTenantRequest(Guid TenantId);

public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? TenantRole,
    Guid? TenantId,
    string? TenantName,
    bool IsSuperAdmin);

public record TenantSummaryDto(Guid Id, string Name, string Slug, string Role);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordWithTokenRequest(string Email, string Token, string NewPassword);

namespace PeruShopHub.Application.DTOs.Settings;

public record UserDetailDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTime? LastLogin,
    DateTime CreatedAt);

public record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    string Role);

public record UpdateUserRequest(
    string Name,
    string Email,
    string Role);

public record ResetPasswordRequest(string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

namespace PeruShopHub.Application.DTOs.Profile;

public record ProfileDto(
    Guid Id,
    string Name,
    string Email,
    string? AvatarUrl,
    DateTime? LastLogin,
    DateTime CreatedAt);

public record UpdateProfileRequest(string Name);

public record UpdateProfileEmailRequest(string NewEmail, string CurrentPassword);

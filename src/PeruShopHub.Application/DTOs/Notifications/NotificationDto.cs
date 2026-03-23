namespace PeruShopHub.Application.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Description,
    DateTime Timestamp,
    bool IsRead,
    string? NavigationTarget);

namespace PeruShopHub.Application.DTOs.AuditLogs;

public record AuditLogDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt
);

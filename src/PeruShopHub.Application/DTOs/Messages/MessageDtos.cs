namespace PeruShopHub.Application.DTOs.Messages;

public record MessageDto(
    Guid Id,
    string ExternalPackId,
    Guid? OrderId,
    string SenderType,
    string Text,
    DateTime SentAt,
    bool IsRead);

public record MessageThreadDto(
    string ExternalPackId,
    Guid? OrderId,
    List<MessageDto> Messages,
    int UnreadCount);

public record SendMessageRequest(string Text);

public record UnreadCountDto(int UnreadCount);

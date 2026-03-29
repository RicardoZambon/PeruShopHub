using PeruShopHub.Application.DTOs.Messages;

namespace PeruShopHub.Application.Services;

public interface IMarketplaceMessageService
{
    Task<MessageThreadDto> GetThreadByOrderAsync(Guid orderId, CancellationToken ct = default);

    Task<MessageDto> SendMessageAsync(Guid orderId, SendMessageRequest request, CancellationToken ct = default);

    Task MarkAsReadAsync(Guid orderId, CancellationToken ct = default);

    Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken ct = default);

    Task SyncMessagesForOrderAsync(Guid orderId, Guid tenantId, CancellationToken ct = default);
}

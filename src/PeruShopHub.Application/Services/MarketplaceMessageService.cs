using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeruShopHub.Application.DTOs.Messages;
using PeruShopHub.Application.Exceptions;
using PeruShopHub.Core.Entities;
using PeruShopHub.Infrastructure.Persistence;

namespace PeruShopHub.Application.Services;

public class MarketplaceMessageService : IMarketplaceMessageService
{
    private readonly PeruShopHubDbContext _db;
    private readonly ILogger<MarketplaceMessageService> _logger;

    public MarketplaceMessageService(
        PeruShopHubDbContext db,
        ILogger<MarketplaceMessageService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MessageThreadDto> GetThreadByOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} not found");

        var messages = await _db.MarketplaceMessages
            .AsNoTracking()
            .Where(m => m.OrderId == orderId)
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageDto(
                m.Id,
                m.ExternalPackId,
                m.OrderId,
                m.SenderType,
                m.Text,
                m.SentAt,
                m.IsRead))
            .ToListAsync(ct);

        var unreadCount = messages.Count(m => !m.IsRead && m.SenderType == "buyer");

        // Use ExternalOrderId as pack ID fallback
        var packId = messages.FirstOrDefault()?.ExternalPackId ?? order.ExternalOrderId;

        return new MessageThreadDto(packId, orderId, messages, unreadCount);
    }

    public async Task<MessageDto> SendMessageAsync(Guid orderId, SendMessageRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} not found");

        var message = new MarketplaceMessage
        {
            Id = Guid.NewGuid(),
            ExternalPackId = order.ExternalOrderId,
            OrderId = orderId,
            SenderType = "seller",
            Text = request.Text,
            SentAt = DateTime.UtcNow,
            IsRead = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.MarketplaceMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Message sent for order {OrderId}", orderId);

        return new MessageDto(
            message.Id,
            message.ExternalPackId,
            message.OrderId,
            message.SenderType,
            message.Text,
            message.SentAt,
            message.IsRead);
    }

    public async Task MarkAsReadAsync(Guid orderId, CancellationToken ct = default)
    {
        var unreadMessages = await _db.MarketplaceMessages
            .Where(m => m.OrderId == orderId && !m.IsRead && m.SenderType == "buyer")
            .ToListAsync(ct);

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.UpdatedAt = DateTime.UtcNow;
        }

        if (unreadMessages.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken ct = default)
    {
        var count = await _db.MarketplaceMessages
            .AsNoTracking()
            .Where(m => !m.IsRead && m.SenderType == "buyer")
            .CountAsync(ct);

        return new UnreadCountDto(count);
    }

    public async Task SyncMessagesForOrderAsync(Guid orderId, Guid tenantId, CancellationToken ct = default)
    {
        // Placeholder for ML API message sync — will be wired when ML adapter supports messages
        _logger.LogInformation("SyncMessagesForOrderAsync called for order {OrderId}, tenant {TenantId}", orderId, tenantId);
    }
}

using Microsoft.AspNetCore.SignalR;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.API.Hubs;

public class NotificationHubContextAdapter : INotificationHubContext
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubContextAdapter(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(object notification, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, ct);
    }

    public async Task SendDataChangeAsync(string entityType, string action, string entityId, CancellationToken ct = default)
    {
        await _hubContext.Clients.All.SendAsync("DataChanged", new { entityType, action, entityId }, ct);
    }
}

using PeruShopHub.Core.Enums;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class NotificationPreference : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public SystemUser User { get; set; } = null!;
}

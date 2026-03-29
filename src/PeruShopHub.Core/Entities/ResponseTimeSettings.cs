using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class ResponseTimeSettings : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public int QuestionThresholdHours { get; set; } = 4;
    public int MessageThresholdHours { get; set; } = 12;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

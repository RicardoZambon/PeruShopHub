using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class ReportSchedule : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string Frequency { get; set; } = "weekly"; // weekly | monthly
    public string Recipients { get; set; } = string.Empty; // comma-separated emails
    public bool IsActive { get; set; } = true;
    public DateTime? LastSentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

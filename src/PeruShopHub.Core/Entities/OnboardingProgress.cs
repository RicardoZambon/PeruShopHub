using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class OnboardingProgress : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public List<string> StepsCompleted { get; set; } = new();
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

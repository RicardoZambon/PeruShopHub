namespace PeruShopHub.Core.Entities;

public class AccountDeletionRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Cancelled, Completed
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ScheduledDeletionAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public SystemUser User { get; set; } = null!;
}

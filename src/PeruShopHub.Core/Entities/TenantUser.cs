namespace PeruShopHub.Core.Entities;

public class TenantUser
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Viewer";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public SystemUser User { get; set; } = null!;
}

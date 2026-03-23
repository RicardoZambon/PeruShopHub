namespace PeruShopHub.Core.Entities;

public class SystemUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

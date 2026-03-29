namespace PeruShopHub.Core.Entities;

public class SystemUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TermsAcceptedAt { get; set; }
    public DateTime? PrivacyAcceptedAt { get; set; }

    public ICollection<TenantUser> TenantMemberships { get; set; } = new List<TenantUser>();
}

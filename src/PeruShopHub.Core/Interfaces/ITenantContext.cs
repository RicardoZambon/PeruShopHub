namespace PeruShopHub.Core.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsSuperAdmin { get; }
    void Set(Guid? tenantId, bool isSuperAdmin);
}

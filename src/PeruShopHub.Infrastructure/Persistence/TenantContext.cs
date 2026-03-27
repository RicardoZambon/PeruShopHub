using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Persistence;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public bool IsSuperAdmin { get; private set; }

    public void Set(Guid? tenantId, bool isSuperAdmin)
    {
        TenantId = tenantId;
        IsSuperAdmin = isSuperAdmin;
    }
}

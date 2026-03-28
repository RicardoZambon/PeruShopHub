using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class TaxProfile : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string TaxRegime { get; set; } = "SimplesNacional"; // SimplesNacional, LucroPresumido, MEI
    public decimal AliquotPercentage { get; set; } = 6.0m;
    public string? State { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

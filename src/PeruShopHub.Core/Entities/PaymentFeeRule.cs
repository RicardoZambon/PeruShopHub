using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class PaymentFeeRule : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public int InstallmentMin { get; set; } = 1;
    public int InstallmentMax { get; set; } = 1;
    public decimal FeePercentage { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

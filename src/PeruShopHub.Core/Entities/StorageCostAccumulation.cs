using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class StorageCostAccumulation : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public DateTime Date { get; set; }
    public decimal DailyCost { get; set; }
    public decimal CumulativeCost { get; set; }
    public int DaysStored { get; set; }
    public string SizeCategory { get; set; } = string.Empty;
    public decimal PenaltyMultiplier { get; set; } = 1m;
    public Product Product { get; set; } = null!;
}

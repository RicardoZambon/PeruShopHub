using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class OrderCost : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public string Source { get; set; } = "Manual";
    public bool IsZeroValue { get; set; }
    public Order Order { get; set; } = null!;
}

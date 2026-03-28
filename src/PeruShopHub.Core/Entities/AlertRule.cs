using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class AlertRule : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }

    /// <summary>
    /// MarginBelow, CostIncrease, StockLow
    /// </summary>
    public string Type { get; set; } = "MarginBelow";

    /// <summary>
    /// Threshold value (percentage for margin/cost, units for stock)
    /// </summary>
    public decimal Threshold { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional: applies to a specific product. Null = all products.
    /// </summary>
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

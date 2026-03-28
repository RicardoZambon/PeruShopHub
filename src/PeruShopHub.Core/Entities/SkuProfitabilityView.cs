using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

/// <summary>
/// Read-only entity mapped to the sku_profitability materialized view.
/// </summary>
public class SkuProfitabilityView : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CostCmv { get; set; }
    public decimal CostCommissions { get; set; }
    public decimal CostShipping { get; set; }
    public decimal CostTaxes { get; set; }
    public decimal CostOther { get; set; }
    public decimal TotalCosts { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal AvgMargin { get; set; }
}

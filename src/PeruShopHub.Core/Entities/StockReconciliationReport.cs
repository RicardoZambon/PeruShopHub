using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

/// <summary>
/// Result of a periodic stock reconciliation between local DB and marketplace API.
/// Created by StockReconciliationWorker every 6 hours.
/// </summary>
public class StockReconciliationReport : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string MarketplaceId { get; set; } = "mercadolivre";
    public int ItemsChecked { get; set; }
    public int Matches { get; set; }
    public int Discrepancies { get; set; }
    public int AutoCorrected { get; set; }
    public int ManualReviewRequired { get; set; }
    public string Status { get; set; } = "Completed"; // Running, Completed, Failed
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<StockReconciliationReportItem> Items { get; set; } = new();
}

/// <summary>
/// Individual item comparison within a reconciliation report.
/// </summary>
public class StockReconciliationReportItem : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public Guid ProductVariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public int LocalQuantity { get; set; }
    public int MarketplaceQuantity { get; set; }
    public int Difference { get; set; }
    public string Resolution { get; set; } = "Match"; // Match, AutoCorrected, ManualReview
    public string? Notes { get; set; }
    public StockReconciliationReport Report { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}

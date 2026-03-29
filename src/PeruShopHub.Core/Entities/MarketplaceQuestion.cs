using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Core.Entities;

public class MarketplaceQuestion : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string ExternalItemId { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public string Status { get; set; } = "UNANSWERED";
    public DateTime QuestionDate { get; set; }
    public DateTime? AnswerDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

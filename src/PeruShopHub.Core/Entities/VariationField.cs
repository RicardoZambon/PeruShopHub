namespace PeruShopHub.Core.Entities;

public class VariationField
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "select"; // "text" or "select"
    public string[] Options { get; set; } = [];
    public bool Required { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Category Category { get; set; } = null!;
}

namespace PeruShopHub.Core.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public int ProductCount { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}

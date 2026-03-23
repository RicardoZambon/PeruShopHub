namespace PeruShopHub.Core.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastPurchase { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

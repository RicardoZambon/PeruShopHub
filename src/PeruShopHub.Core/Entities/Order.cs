namespace PeruShopHub.Core.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerNickname { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Profit { get; set; }
    public string Status { get; set; } = "Pago";
    public DateTime OrderDate { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? LogisticType { get; set; }
    public string? PaymentMethod { get; set; }
    public int? Installments { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? PaymentStatus { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Customer? Customer { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderCost> Costs { get; set; } = new List<OrderCost>();
}

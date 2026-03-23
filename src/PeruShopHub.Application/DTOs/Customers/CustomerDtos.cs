namespace PeruShopHub.Application.DTOs.Customers;

public record CustomerListDto(
    Guid Id,
    string Name,
    string? Nickname,
    string? Email,
    string? Phone,
    int TotalOrders,
    decimal TotalSpent,
    DateTime? LastPurchase);

public record CustomerDetailDto(
    Guid Id,
    string Name,
    string? Nickname,
    string? Email,
    string? Phone,
    int TotalOrders,
    decimal TotalSpent,
    DateTime? LastPurchase,
    DateTime CreatedAt,
    IReadOnlyList<CustomerOrderDto> RecentOrders);

public record CustomerOrderDto(
    Guid Id,
    string ExternalOrderId,
    decimal TotalAmount,
    string Status,
    DateTime OrderDate);

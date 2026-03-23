namespace PeruShopHub.Application.DTOs.Orders;

public record OrderListDto(
    Guid Id,
    string ExternalOrderId,
    string BuyerName,
    int ItemCount,
    decimal TotalAmount,
    decimal Profit,
    string Status,
    DateTime OrderDate,
    string? TrackingNumber);

public record OrderDetailDto(
    Guid Id,
    string ExternalOrderId,
    BuyerDto Buyer,
    int ItemCount,
    decimal TotalAmount,
    decimal Profit,
    string Status,
    DateTime OrderDate,
    ShippingInfoDto Shipping,
    PaymentInfoDto Payment,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderCostDto> Costs);

public record OrderItemDto(
    Guid Id,
    Guid? ProductId,
    string Name,
    string Sku,
    string? Variation,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public record OrderCostDto(
    Guid Id,
    string Category,
    string? Description,
    decimal Value,
    string Source);

public record BuyerDto(
    string Name,
    string? Nickname,
    string? Email,
    string? Phone);

public record ShippingInfoDto(
    string? TrackingNumber,
    string? Carrier,
    string? LogisticType,
    IReadOnlyList<TimelineStepDto>? Timeline);

public record TimelineStepDto(
    string Status,
    DateTime? Timestamp,
    string? Description);

public record PaymentInfoDto(
    string? Method,
    int? Installments,
    decimal? Amount,
    string? Status);

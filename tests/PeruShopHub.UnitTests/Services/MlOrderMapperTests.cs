using FluentAssertions;
using PeruShopHub.Application.Services;
using PeruShopHub.Core.Entities;
using PeruShopHub.Core.Interfaces;
using Xunit;

namespace PeruShopHub.UnitTests.Services;

public class MlOrderMapperTests
{
    private readonly MlOrderMapper _mapper = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── Helper: build a standard ML order details ──────────────

    private static MarketplaceOrderDetails CreateOrderDetails(
        string status = "paid",
        decimal totalAmount = 199.90m,
        int itemCount = 1,
        string buyerNickname = "COMPRADOR_TEST",
        string? buyerEmail = "comprador@test.com",
        string? shippingId = "SHP-12345") =>
        new(
            ExternalOrderId: "MLB123456789",
            Status: status,
            DateCreated: new DateTimeOffset(2026, 3, 15, 14, 30, 0, TimeSpan.Zero),
            TotalAmount: totalAmount,
            Buyer: new MarketplaceBuyer("999888777", buyerNickname, buyerEmail),
            Items: Enumerable.Range(1, itemCount).Select(i =>
                new MarketplaceOrderItem($"MLB-ITEM-{i}", $"Produto Teste {i}", 1, totalAmount / itemCount)
            ).ToList(),
            Shipping: shippingId is not null
                ? new MarketplaceShipping(shippingId, "ready_to_ship", 15.90m)
                : null
        );

    // ── 1. Paid order — full field mapping ─────────────────────

    [Fact]
    public void MapOrderDetails_PaidOrder_MapsAllFields()
    {
        var order = new Order { Id = Guid.NewGuid() };
        var details = CreateOrderDetails(status: "paid", totalAmount: 249.90m);

        _mapper.MapOrderDetails(order, details, _tenantId);

        order.TenantId.Should().Be(_tenantId);
        order.ExternalOrderId.Should().Be("MLB123456789");
        order.BuyerName.Should().Be("COMPRADOR_TEST");
        order.BuyerNickname.Should().Be("COMPRADOR_TEST");
        order.BuyerEmail.Should().Be("comprador@test.com");
        order.TotalAmount.Should().Be(249.90m);
        order.ItemCount.Should().Be(1);
        order.OrderDate.Should().Be(new DateTime(2026, 3, 15, 14, 30, 0, DateTimeKind.Utc));
        order.Status.Should().Be("Pago");
        order.LogisticType.Should().Be("mercadolivre");
    }

    // ── 2. Cancelled order — status + fulfillment reset ────────

    [Fact]
    public void MapOrderDetails_CancelledOrder_SetsStatusAndResetsFulfillment()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            IsFulfilled = true,
            FulfilledAt = DateTime.UtcNow
        };
        var details = CreateOrderDetails(status: "cancelled");

        _mapper.MapOrderDetails(order, details, _tenantId);

        order.Status.Should().Be("Cancelado");
        order.IsFulfilled.Should().BeFalse();
        order.FulfilledAt.Should().BeNull();
    }

    // ── 3. Multiple items — quantity sum + subtotal calc ───────

    [Fact]
    public void MapOrderItems_MultipleItems_CalculatesCorrectly()
    {
        var order = new Order { Id = Guid.NewGuid() };
        var items = new List<MarketplaceOrderItem>
        {
            new("MLB-ITEM-1", "Produto A", 2, 50.00m),
            new("MLB-ITEM-2", "Produto B", 3, 30.00m),
            new("MLB-ITEM-3", "Produto C", 1, 89.90m)
        };

        var result = _mapper.MapOrderItems(order, items, _tenantId);

        result.Should().HaveCount(3);
        result[0].Subtotal.Should().Be(100.00m); // 2 × 50
        result[1].Subtotal.Should().Be(90.00m);  // 3 × 30
        result[2].Subtotal.Should().Be(89.90m);  // 1 × 89.90
        order.ItemCount.Should().Be(6); // 2 + 3 + 1
        result.Should().OnlyContain(i => i.TenantId == _tenantId);
        result.Should().OnlyContain(i => i.OrderId == order.Id);
    }

    // ── 4. Status mapping — all ML statuses ────────────────────

    [Theory]
    [InlineData("paid", "Pago")]
    [InlineData("confirmed", "Pago")]
    [InlineData("payment_required", "Aguardando Pagamento")]
    [InlineData("payment_in_process", "Aguardando Pagamento")]
    [InlineData("cancelled", "Cancelado")]
    [InlineData("invalid", "Cancelado")]
    [InlineData("partially_refunded", "Reembolso Parcial")]
    [InlineData("PAID", "Pago")]           // case insensitive
    [InlineData("Cancelled", "Cancelado")] // mixed case
    [InlineData("unknown_status", "Pago")] // unknown defaults to Pago
    public void MapOrderStatus_ReturnsCorrectInternalStatus(string mlStatus, string expected)
    {
        _mapper.MapOrderStatus(mlStatus).Should().Be(expected);
    }

    // ── 5. Fulfilled status check ──────────────────────────────

    [Theory]
    [InlineData("paid", true)]
    [InlineData("confirmed", true)]
    [InlineData("cancelled", false)]
    [InlineData("payment_required", false)]
    [InlineData("invalid", false)]
    [InlineData("unknown", false)]
    public void IsFulfilledStatus_ReturnsCorrectResult(string mlStatus, bool expected)
    {
        _mapper.IsFulfilledStatus(mlStatus).Should().Be(expected);
    }

    // ── 6. Fee type → cost category mapping ────────────────────

    [Theory]
    [InlineData("sale_fee", "marketplace_commission")]
    [InlineData("marketplace_fee", "marketplace_commission")]
    [InlineData("shipping", "shipping_seller")]
    [InlineData("shipping_fee", "shipping_seller")]
    [InlineData("financing_fee", "payment_fee")]
    [InlineData("financing", "payment_fee")]
    [InlineData("fixed_fee", "fixed_fee")]
    [InlineData("SALE_FEE", "marketplace_commission")]  // case insensitive
    [InlineData("custom_fee", "custom_fee")]            // unknown passthrough
    public void MapFeeTypeToCategory_ReturnsCorrectCategory(string feeType, string expected)
    {
        _mapper.MapFeeTypeToCategory(feeType).Should().Be(expected);
    }

    // ── 7. Fee mapping — multiple fees with abs values ─────────

    [Fact]
    public void MapFeesToCosts_MultipleFees_CreatesCorrectCosts()
    {
        var order = new Order { Id = Guid.NewGuid() };
        var fees = new List<MarketplaceFee>
        {
            new("sale_fee", -25.99m, "BRL"),
            new("shipping", -15.50m, "BRL"),
            new("fixed_fee", -6.50m, "BRL"),
            new("financing_fee", -3.20m, "BRL")
        };

        var costs = _mapper.MapFeesToCosts(order, fees, _tenantId);

        costs.Should().HaveCount(4);

        costs.Should().Contain(c => c.Category == "marketplace_commission" && c.Value == 25.99m);
        costs.Should().Contain(c => c.Category == "shipping_seller" && c.Value == 15.50m);
        costs.Should().Contain(c => c.Category == "fixed_fee" && c.Value == 6.50m);
        costs.Should().Contain(c => c.Category == "payment_fee" && c.Value == 3.20m);

        costs.Should().OnlyContain(c => c.Source == "API");
        costs.Should().OnlyContain(c => c.OrderId == order.Id);
        costs.Should().OnlyContain(c => c.TenantId == _tenantId);
    }

    // ── 8. Buyer → Customer mapping ────────────────────────────

    [Fact]
    public void MapBuyerToCustomer_CreatesCustomerWithCorrectFields()
    {
        var buyer = new MarketplaceBuyer("EXT-123", "BUYER_NICK", "buyer@example.com");

        var customer = _mapper.MapBuyerToCustomer(buyer, _tenantId);

        customer.TenantId.Should().Be(_tenantId);
        customer.Name.Should().Be("BUYER_NICK");
        customer.Nickname.Should().Be("BUYER_NICK");
        customer.Email.Should().Be("buyer@example.com");
        customer.TotalOrders.Should().Be(1);
        customer.Id.Should().NotBeEmpty();
    }

    // ── 9. Resource path extraction ────────────────────────────

    [Theory]
    [InlineData("/orders/123456789", "123456789")]
    [InlineData("orders/123456789", "123456789")]
    [InlineData("/shipments/987654321", "987654321")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("single", null)]  // no slash separator
    public void ExtractOrderIdFromResource_ReturnsCorrectId(string? resource, string? expected)
    {
        _mapper.ExtractOrderIdFromResource(resource).Should().Be(expected);
    }

    // ── 10. Profit calculation ─────────────────────────────────

    [Fact]
    public void CalculateProfit_WithCosts_ReturnsCorrectProfit()
    {
        var costs = new List<OrderCost>
        {
            new() { Value = 25.99m },
            new() { Value = 15.50m },
            new() { Value = 6.50m },
            new() { Value = 3.20m }
        };

        var profit = _mapper.CalculateProfit(199.90m, costs);

        profit.Should().Be(199.90m - 51.19m);
        profit.Should().Be(148.71m);
    }

    [Fact]
    public void CalculateProfit_NoCosts_ReturnsTotalAmount()
    {
        _mapper.CalculateProfit(199.90m, []).Should().Be(199.90m);
    }

    // ── 11. No shipping — LogisticType stays null ──────────────

    [Fact]
    public void MapOrderDetails_NoShipping_LogisticTypeIsNull()
    {
        var order = new Order { Id = Guid.NewGuid() };
        var details = CreateOrderDetails(shippingId: null);

        _mapper.MapOrderDetails(order, details, _tenantId);

        order.LogisticType.Should().BeNull();
    }

    // ── 12. Invalid order — maps to Cancelado ──────────────────

    [Fact]
    public void MapOrderDetails_InvalidStatus_MapsToCancelado()
    {
        var order = new Order { Id = Guid.NewGuid() };
        var details = CreateOrderDetails(status: "invalid");

        _mapper.MapOrderDetails(order, details, _tenantId);

        order.Status.Should().Be("Cancelado");
        order.IsFulfilled.Should().BeFalse();
        order.FulfilledAt.Should().BeNull();
    }

    // ── 13. Payment in process — awaiting status ───────────────

    [Fact]
    public void MapOrderDetails_PaymentInProcess_MapsToAguardando()
    {
        var order = new Order { Id = Guid.NewGuid() };
        var details = CreateOrderDetails(status: "payment_in_process");

        _mapper.MapOrderDetails(order, details, _tenantId);

        order.Status.Should().Be("Aguardando Pagamento");
    }

    // ── 14. Empty fees list — returns empty costs ──────────────

    [Fact]
    public void MapFeesToCosts_EmptyFees_ReturnsEmptyList()
    {
        var order = new Order { Id = Guid.NewGuid() };

        var costs = _mapper.MapFeesToCosts(order, [], _tenantId);

        costs.Should().BeEmpty();
    }

    // ── 15. Buyer with null email — customer still created ─────

    [Fact]
    public void MapBuyerToCustomer_NullEmail_CreatesCustomerWithoutEmail()
    {
        var buyer = new MarketplaceBuyer("EXT-456", "NO_EMAIL_BUYER", null);

        var customer = _mapper.MapBuyerToCustomer(buyer, _tenantId);

        customer.Email.Should().BeNull();
        customer.Name.Should().Be("NO_EMAIL_BUYER");
    }
}

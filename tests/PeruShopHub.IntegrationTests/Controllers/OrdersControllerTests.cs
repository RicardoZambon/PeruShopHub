using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Orders;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.Core.Entities;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class OrdersControllerTests : IntegrationTestBase
{
    public OrdersControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(AuthResponse Auth, Guid TenantId)> AuthenticateAndGetTenant(string email)
    {
        var registerRequest = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (auth, auth.User.TenantId!.Value);
    }

    [Fact]
    public async Task Order_AddCosts_Fulfill_VerifyFlow()
    {
        var (auth, tenantId) = await AuthenticateAndGetTenant("order-flow@test.com");

        // Create a product with a variant that has stock
        var createProductDto = new CreateProductDto(
            Sku: "ORD-PROD-001", Name: "Order Test Product", Description: null,
            CategoryId: null, Price: 100.00m, PurchaseCost: 40.00m,
            PackagingCost: 2.00m, Supplier: null,
            Weight: 1m, Height: 10m, Width: 10m, Length: 10m);
        var productResponse = await Client.PostAsJsonAsync("/api/products", createProductDto);
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        // Seed an order directly in the DB (orders come from marketplace webhooks, not frontend)
        var orderId = Guid.NewGuid();
        using (var db = CreateDbContext())
        {
            // Get the default variant
            var variant = await db.ProductVariants
                .FirstOrDefaultAsync(v => v.ProductId == product!.Id);

            // Give the variant some stock
            if (variant != null)
            {
                variant.Stock = 50;
                await db.SaveChangesAsync();
            }

            var order = new Order
            {
                Id = orderId,
                TenantId = tenantId,
                ExternalOrderId = "ML-123456",
                BuyerName = "João Silva",
                BuyerEmail = "joao@test.com",
                ItemCount = 1,
                TotalAmount = 100.00m,
                Status = "Pago",
                OrderDate = DateTime.UtcNow,
                PaymentMethod = "credit_card",
                PaymentStatus = "approved",
                CreatedAt = DateTime.UtcNow
            };
            db.Orders.Add(order);

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderId = orderId,
                ProductId = product!.Id,
                Name = product.Name,
                Sku = product.Sku,
                Quantity = 2,
                UnitPrice = 50.00m,
                Subtotal = 100.00m
            };
            db.OrderItems.Add(orderItem);
            await db.SaveChangesAsync();
        }

        // 1. Get order detail
        var orderDetailResponse = await Client.GetAsync($"/api/orders/{orderId}");
        orderDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDetail = await orderDetailResponse.Content.ReadFromJsonAsync<OrderDetailDto>();
        orderDetail.Should().NotBeNull();
        orderDetail!.ExternalOrderId.Should().Be("ML-123456");
        orderDetail.Items.Should().HaveCount(1);

        // 2. Add a cost
        var addCostRequest = new CreateOrderCostRequest("shipping_seller", "Frete vendedor", 15.50m);
        var addCostResponse = await Client.PostAsJsonAsync($"/api/orders/{orderId}/costs", addCostRequest);
        addCostResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var cost = await addCostResponse.Content.ReadFromJsonAsync<OrderCostDto>();
        cost.Should().NotBeNull();
        cost!.Category.Should().Be("shipping_seller");
        cost.Value.Should().Be(15.50m);

        // 3. Fulfill order
        var fulfillResponse = await Client.PostAsync($"/api/orders/{orderId}/fulfill", null);
        fulfillResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Verify order is fulfilled and stock decreased
        var afterFulfillResponse = await Client.GetAsync($"/api/orders/{orderId}");
        afterFulfillResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterFulfill = await afterFulfillResponse.Content.ReadFromJsonAsync<OrderDetailDto>();
        afterFulfill!.IsFulfilled.Should().BeTrue();

        // Verify stock decreased via DB
        using (var db = CreateDbContext())
        {
            var variant = await db.ProductVariants
                .FirstOrDefaultAsync(v => v.ProductId == product!.Id);
            variant.Should().NotBeNull();
            variant!.Stock.Should().BeLessThan(50); // stock decreased from fulfill
        }
    }

    [Fact]
    public async Task Order_GetList_Pagination()
    {
        var (_, tenantId) = await AuthenticateAndGetTenant("order-list@test.com");

        // Seed 3 orders
        using (var db = CreateDbContext())
        {
            for (int i = 1; i <= 3; i++)
            {
                db.Orders.Add(new Order
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ExternalOrderId = $"ML-LIST-{i}",
                    BuyerName = $"Buyer {i}",
                    ItemCount = 1,
                    TotalAmount = 100m * i,
                    Status = "Pago",
                    OrderDate = DateTime.UtcNow.AddDays(-i),
                    CreatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/orders?page=1&pageSize=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ML-LIST-");
    }
}

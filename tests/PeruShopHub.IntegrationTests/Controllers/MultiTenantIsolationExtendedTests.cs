using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Categories;
using PeruShopHub.Application.DTOs.Supplies;
using PeruShopHub.Core.Entities;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class MultiTenantIsolationExtendedTests : IntegrationTestBase
{
    public MultiTenantIsolationExtendedTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(AuthResponse Auth, Guid TenantId)> RegisterAndAuth(HttpClient client, string shopName, string email)
    {
        var req = new RegisterRequest(shopName, "Test User", email, "Password123!");
        var res = await client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (auth, auth.User.TenantId!.Value);
    }

    [Fact]
    public async Task TenantIsolation_Categories()
    {
        var clientA = Factory.CreateClient();
        var clientB = Factory.CreateClient();

        try
        {
            await RegisterAndAuth(clientA, "Cat Tenant A", "cat-iso-a@test.com");
            await RegisterAndAuth(clientB, "Cat Tenant B", "cat-iso-b@test.com");

            // A creates a category
            var catDto = new CreateCategoryDto("Cat A Only", "cat-a-only", null, null, 1, null);
            var createRes = await clientA.PostAsJsonAsync("/api/categories", catDto);
            createRes.EnsureSuccessStatusCode();
            var catA = await createRes.Content.ReadFromJsonAsync<CategoryDetailDto>();

            // B should not see it
            var listRes = await clientB.GetAsync("/api/categories");
            var content = await listRes.Content.ReadAsStringAsync();
            content.Should().NotContain("Cat A Only");

            // B tries to access by ID - should get 404
            var crossRes = await clientB.GetAsync($"/api/categories/{catA!.Id}");
            crossRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            clientA.Dispose();
            clientB.Dispose();
        }
    }

    [Fact]
    public async Task TenantIsolation_Supplies()
    {
        var clientA = Factory.CreateClient();
        var clientB = Factory.CreateClient();

        try
        {
            await RegisterAndAuth(clientA, "Sup Tenant A", "sup-iso-a@test.com");
            await RegisterAndAuth(clientB, "Sup Tenant B", "sup-iso-b@test.com");

            // A creates a supply
            var supDto = new CreateSupplyDto("Supply A Only", "SUP-ISO-A", "Embalagem", 5.00m, 100, 10, null);
            var createRes = await clientA.PostAsJsonAsync("/api/supplies", supDto);
            createRes.EnsureSuccessStatusCode();

            // B should not see it
            var listRes = await clientB.GetAsync("/api/supplies");
            var content = await listRes.Content.ReadAsStringAsync();
            content.Should().NotContain("Supply A Only");
        }
        finally
        {
            clientA.Dispose();
            clientB.Dispose();
        }
    }

    [Fact]
    public async Task TenantIsolation_Orders()
    {
        var clientA = Factory.CreateClient();
        var clientB = Factory.CreateClient();

        try
        {
            var (_, tenantA) = await RegisterAndAuth(clientA, "Order Tenant A", "ord-iso-a@test.com");
            await RegisterAndAuth(clientB, "Order Tenant B", "ord-iso-b@test.com");

            // Seed an order for tenant A
            var orderId = Guid.NewGuid();
            using (var db = CreateDbContext())
            {
                db.Orders.Add(new Order
                {
                    Id = orderId,
                    TenantId = tenantA,
                    ExternalOrderId = "ISO-ORDER-A",
                    BuyerName = "Buyer A",
                    ItemCount = 1,
                    TotalAmount = 100m,
                    Status = "Pago",
                    OrderDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            // B should not see A's order
            var crossRes = await clientB.GetAsync($"/api/orders/{orderId}");
            crossRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            clientA.Dispose();
            clientB.Dispose();
        }
    }
}

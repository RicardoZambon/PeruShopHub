using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Core.Entities;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class CustomersControllerTests : IntegrationTestBase
{
    public CustomersControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<Guid> Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return auth.User.TenantId!.Value;
    }

    [Fact]
    public async Task Customers_GetList_ReturnsOk()
    {
        var tenantId = await Authenticate("customers-list@test.com");

        // Seed a customer via DB (customers come from orders/webhooks)
        using (var db = CreateDbContext())
        {
            db.Customers.Add(new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Maria Silva",
                Email = "maria@test.com",
                TotalOrders = 3,
                TotalSpent = 450.00m,
                LastPurchase = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var res = await Client.GetAsync("/api/customers?page=1&pageSize=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await res.Content.ReadAsStringAsync();
        content.Should().Contain("Maria Silva");
    }
}

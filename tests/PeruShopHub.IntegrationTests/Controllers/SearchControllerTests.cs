using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Products;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class SearchControllerTests : IntegrationTestBase
{
    public SearchControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Search_ReturnsOk()
    {
        await Authenticate("search-test@test.com");

        // Create a product to search for
        var productDto = new CreateProductDto("SRCH-001", "Searchable Product", null, null, 50.00m, 20.00m, 1.00m, null, null, 1m, 10m, 10m, 10m);
        await Client.PostAsJsonAsync("/api/products", productDto);

        var res = await Client.GetAsync("/api/search?q=Searchable&limit=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

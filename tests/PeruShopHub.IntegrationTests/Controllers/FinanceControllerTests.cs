using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class FinanceControllerTests : IntegrationTestBase
{
    public FinanceControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Finance_Summary_ReturnsOk()
    {
        await Authenticate("finance-summary@test.com");
        var res = await Client.GetAsync("/api/finance/summary");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Finance_Charts_ReturnOk()
    {
        await Authenticate("finance-charts@test.com");

        var revenueRes = await Client.GetAsync("/api/finance/chart/revenue-profit?days=30");
        revenueRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var marginRes = await Client.GetAsync("/api/finance/chart/margin?days=30");
        marginRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Finance_SkuProfitability_ReturnsOk()
    {
        await Authenticate("finance-sku@test.com");
        var res = await Client.GetAsync("/api/finance/sku-profitability?page=1&pageSize=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Finance_Reconciliation_ReturnsOk()
    {
        await Authenticate("finance-recon@test.com");
        var res = await Client.GetAsync("/api/finance/reconciliation?year=2026");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Finance_AbcCurve_ReturnsOk()
    {
        await Authenticate("finance-abc@test.com");
        var res = await Client.GetAsync("/api/finance/abc-curve");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

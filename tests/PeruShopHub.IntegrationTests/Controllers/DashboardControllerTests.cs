using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class DashboardControllerTests : IntegrationTestBase
{
    public DashboardControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Dashboard_Summary_ReturnsOk()
    {
        await Authenticate("dashboard@test.com");
        var res = await Client.GetAsync("/api/dashboard/summary");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Dashboard_Charts_ReturnOk()
    {
        await Authenticate("dashboard-charts@test.com");

        var revenueRes = await Client.GetAsync("/api/dashboard/chart/revenue-profit?days=30");
        revenueRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var costRes = await Client.GetAsync("/api/dashboard/chart/cost-breakdown");
        costRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var topRes = await Client.GetAsync("/api/dashboard/top-products");
        topRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var leastRes = await Client.GetAsync("/api/dashboard/least-profitable");
        leastRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var pendingRes = await Client.GetAsync("/api/dashboard/pending-actions");
        pendingRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

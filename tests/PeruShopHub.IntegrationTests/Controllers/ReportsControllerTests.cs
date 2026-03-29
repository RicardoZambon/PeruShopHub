using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class ReportsControllerTests : IntegrationTestBase
{
    public ReportsControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Reports_ProfitabilityPdf_ReturnsFile()
    {
        await Authenticate("reports-profit-pdf@test.com");
        var res = await Client.GetAsync("/api/reports/profitability/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Reports_OrdersPdf_ReturnsFile()
    {
        await Authenticate("reports-orders-pdf@test.com");
        var res = await Client.GetAsync("/api/reports/orders/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Reports_InventoryPdf_ReturnsFile()
    {
        await Authenticate("reports-inv-pdf@test.com");
        var res = await Client.GetAsync("/api/reports/inventory/pdf");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Reports_ProfitabilityExcel_ReturnsFile()
    {
        await Authenticate("reports-profit-excel@test.com");
        var res = await Client.GetAsync("/api/reports/profitability/excel");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should().Contain("spreadsheet");
    }

    [Fact]
    public async Task Reports_OrdersExcel_ReturnsFile()
    {
        await Authenticate("reports-orders-excel@test.com");
        var res = await Client.GetAsync("/api/reports/orders/excel");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should().Contain("spreadsheet");
    }

    [Fact]
    public async Task Reports_InventoryExcel_ReturnsFile()
    {
        await Authenticate("reports-inv-excel@test.com");
        var res = await Client.GetAsync("/api/reports/inventory/excel");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should().Contain("spreadsheet");
    }

    [Fact]
    public async Task Reports_AccountingExport_ReturnsFile()
    {
        await Authenticate("reports-accounting@test.com");
        var res = await Client.GetAsync("/api/reports/accounting-export?format=bling");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

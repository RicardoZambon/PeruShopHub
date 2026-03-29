using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Supplies;
using PeruShopHub.Application.Common;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class SuppliesControllerTests : IntegrationTestBase
{
    public SuppliesControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Supply_CRUD_Flow()
    {
        await Authenticate("supplies-crud@test.com");

        // Create
        var createDto = new CreateSupplyDto("Caixa Papelão", "SUP-001", "Embalagem", 2.50m, 100, 20, "Fornecedor X");
        var createRes = await Client.PostAsJsonAsync("/api/supplies", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Get list
        var listRes = await Client.GetAsync("/api/supplies?page=1&pageSize=10");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Read content to verify
        var content = await listRes.Content.ReadAsStringAsync();
        content.Should().Contain("SUP-001");

        // Get by ID (extract from list)
        var list = await Client.GetFromJsonAsync<PagedResult<SupplyListDto>>("/api/supplies?search=SUP-001");
        list.Should().NotBeNull();
        list!.Items.Should().NotBeEmpty();
        var supplyId = list.Items[0].Id;

        // Get detail
        var detailRes = await Client.GetAsync($"/api/supplies/{supplyId}");
        detailRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailRes.Content.ReadFromJsonAsync<SupplyDetailDto>();
        detail.Should().NotBeNull();
        detail!.Name.Should().Be("Caixa Papelão");

        // Update
        var updateDto = new UpdateSupplyDto("Caixa Papelão Grande", null, null, 3.00m, null, null, null, null, detail.Version);
        var updateRes = await Client.PutAsJsonAsync($"/api/supplies/{supplyId}", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete
        var deleteRes = await Client.DeleteAsync($"/api/supplies/{supplyId}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

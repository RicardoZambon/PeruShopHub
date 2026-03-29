using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.ResponseTemplates;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class ResponseTemplatesControllerTests : IntegrationTestBase
{
    public ResponseTemplatesControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task ResponseTemplates_CRUD()
    {
        await Authenticate("templates-crud@test.com");

        // Create
        var createDto = new CreateResponseTemplateDto("Agradecimento", "pos_venda", "Obrigado pela compra, {nome}!", "{nome}", 1);
        var createRes = await Client.PostAsJsonAsync("/api/response-templates", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var template = await createRes.Content.ReadFromJsonAsync<ResponseTemplateDetailDto>();
        template.Should().NotBeNull();

        // Get list
        var listRes = await Client.GetAsync("/api/response-templates");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get detail
        var detailRes = await Client.GetAsync($"/api/response-templates/{template!.Id}");
        detailRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update
        var updateDto = new UpdateResponseTemplateDto("Agradecimento V2", null, "Muito obrigado, {nome}!", null, null, null, template.Version);
        var updateRes = await Client.PutAsJsonAsync($"/api/response-templates/{template.Id}", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Increment usage
        var usageRes = await Client.PostAsync($"/api/response-templates/{template.Id}/usage", null);
        usageRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete
        var deleteRes = await Client.DeleteAsync($"/api/response-templates/{template.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

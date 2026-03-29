using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Categories;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class VariationFieldsControllerTests : IntegrationTestBase
{
    public VariationFieldsControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task VariationFields_CRUD()
    {
        await Authenticate("varfields-crud@test.com");

        // Create category first
        var catDto = new CreateCategoryDto("VarField Cat", "varfield-cat", null, null, 1, null);
        var catRes = await Client.PostAsJsonAsync("/api/categories", catDto);
        catRes.EnsureSuccessStatusCode();
        var category = await catRes.Content.ReadFromJsonAsync<CategoryDetailDto>();

        // Create variation field
        var createDto = new CreateVariationFieldDto("Cor", "select", new[] { "Vermelho", "Azul", "Verde" }, true);
        var createRes = await Client.PostAsJsonAsync($"/api/categories/{category!.Id}/variation-fields", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var field = await createRes.Content.ReadFromJsonAsync<VariationFieldDto>();
        field.Should().NotBeNull();

        // Get fields
        var listRes = await Client.GetAsync($"/api/categories/{category.Id}/variation-fields");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get inherited
        var inheritedRes = await Client.GetAsync($"/api/categories/{category.Id}/variation-fields/inherited");
        inheritedRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update
        var updateDto = new UpdateVariationFieldDto("Cor Principal", null, null, null, null);
        var updateRes = await Client.PutAsJsonAsync($"/api/categories/{category.Id}/variation-fields/{field!.Id}", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete
        var deleteRes = await Client.DeleteAsync($"/api/categories/{category.Id}/variation-fields/{field.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

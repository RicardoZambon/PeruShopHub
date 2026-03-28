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
public class CategoriesControllerTests : IntegrationTestBase
{
    public CategoriesControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var registerRequest = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Category_CreateParent_CreateChild_VerifyHierarchy()
    {
        await Authenticate("cat-hierarchy@test.com");

        // 1. Create parent category
        var parentDto = new CreateCategoryDto("Electronics", "electronics", null, "laptop", 1, "ELC");
        var parentResponse = await Client.PostAsJsonAsync("/api/categories", parentDto);
        parentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var parent = await parentResponse.Content.ReadFromJsonAsync<CategoryDetailDto>();
        parent.Should().NotBeNull();
        parent!.Name.Should().Be("Electronics");

        // 2. Create child category
        var childDto = new CreateCategoryDto("Smartphones", "smartphones", parent.Id, "phone", 1, "SMP");
        var childResponse = await Client.PostAsJsonAsync("/api/categories", childDto);
        childResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var child = await childResponse.Content.ReadFromJsonAsync<CategoryDetailDto>();
        child.Should().NotBeNull();
        child!.ParentId.Should().Be(parent.Id);

        // 3. Verify parent has children via detail endpoint
        var parentDetailResponse = await Client.GetAsync($"/api/categories/{parent.Id}");
        parentDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var parentDetail = await parentDetailResponse.Content.ReadFromJsonAsync<CategoryDetailDto>();
        parentDetail!.Children.Should().HaveCount(1);
        parentDetail.Children[0].Name.Should().Be("Smartphones");

        // 4. List root categories (no parentId filter)
        var rootListResponse = await Client.GetAsync("/api/categories");
        rootListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rootList = await rootListResponse.Content.ReadFromJsonAsync<List<CategoryListDto>>();
        rootList.Should().NotBeNull();
        rootList!.Should().Contain(c => c.Name == "Electronics");

        // 5. List children of parent
        var childListResponse = await Client.GetAsync($"/api/categories?parentId={parent.Id}");
        childListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var childList = await childListResponse.Content.ReadFromJsonAsync<List<CategoryListDto>>();
        childList.Should().NotBeNull();
        childList!.Should().HaveCount(1);
        childList[0].Name.Should().Be("Smartphones");
    }

    [Fact]
    public async Task Category_Delete_VerifyCascade()
    {
        await Authenticate("cat-delete@test.com");

        var createDto = new CreateCategoryDto("ToDelete", "to-delete", null, null, 1, null);
        var createResponse = await Client.PostAsJsonAsync("/api/categories", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDetailDto>();

        var deleteResponse = await Client.DeleteAsync($"/api/categories/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDelete = await Client.GetAsync($"/api/categories/{created.Id}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

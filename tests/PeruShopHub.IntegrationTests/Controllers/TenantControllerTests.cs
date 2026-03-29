using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.Application.DTOs.Tenant;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class TenantControllerTests : IntegrationTestBase
{
    public TenantControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Tenant_GetAndUpdate()
    {
        await Authenticate("tenant-test@test.com");

        var getRes = await Client.GetAsync("/api/tenant");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateReq = new UpdateTenantRequest("Loja Atualizada");
        var updateRes = await Client.PutAsJsonAsync("/api/tenant", updateReq);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Tenant_Members_ListAndInvite()
    {
        await Authenticate("tenant-members@test.com");

        // Get members
        var membersRes = await Client.GetAsync("/api/tenant/members");
        membersRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Invite member
        var inviteReq = new CreateUserRequest("New Member", "member@test.com", "Password123!", "Manager");
        var inviteRes = await Client.PostAsJsonAsync("/api/tenant/members/invite", inviteReq);
        inviteRes.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

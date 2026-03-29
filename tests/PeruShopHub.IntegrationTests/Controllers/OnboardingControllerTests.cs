using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Onboarding;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class OnboardingControllerTests : IntegrationTestBase
{
    public OnboardingControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Onboarding_GetProgress_ReturnsOk()
    {
        await Authenticate("onboarding-progress@test.com");
        var res = await Client.GetAsync("/api/onboarding/progress");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Onboarding_CompleteStep_ReturnsOk()
    {
        await Authenticate("onboarding-step@test.com");
        var stepReq = new CompleteStepRequest("create_product");
        var res = await Client.PostAsJsonAsync("/api/onboarding/complete-step", stepReq);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

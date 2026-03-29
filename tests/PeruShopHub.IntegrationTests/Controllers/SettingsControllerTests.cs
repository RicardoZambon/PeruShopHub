using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PeruShopHub.API.Controllers;
using PeruShopHub.Application.DTOs.Auth;
using PeruShopHub.Application.DTOs.Settings;
using PeruShopHub.IntegrationTests.Infrastructure;
using Xunit;

namespace PeruShopHub.IntegrationTests.Controllers;

[Collection("Integration")]
public class SettingsControllerTests : IntegrationTestBase
{
    public SettingsControllerTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task Authenticate(string email)
    {
        var req = new RegisterRequest($"Shop {email}", "Test User", email, "Password123!");
        var res = await Client.PostAsJsonAsync("/api/auth/register", req);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task Settings_CostSettings_GetAndUpdate()
    {
        await Authenticate("settings-costs@test.com");

        var getRes = await Client.GetAsync("/api/settings/costs");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateDto = new UpdateCostsDto(12.5m);
        var updateRes = await Client.PutAsJsonAsync("/api/settings/costs", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_CommissionRules_CRUD()
    {
        await Authenticate("settings-commission@test.com");

        // Get list
        var listRes = await Client.GetAsync("/api/settings/commission-rules");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Create
        var createDto = new CreateCommissionRuleDto("mercadolivre", "Eletrônicos", "classico", 0.13m);
        var createRes = await Client.PostAsJsonAsync("/api/settings/commission-rules", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await createRes.Content.ReadFromJsonAsync<CommissionRuleDto>();
        rule.Should().NotBeNull();

        // Update
        var updateDto = new UpdateCommissionRuleDto(0.15m);
        var updateRes = await Client.PutAsJsonAsync($"/api/settings/commission-rules/{rule!.Id}", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete
        var deleteRes = await Client.DeleteAsync($"/api/settings/commission-rules/{rule.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Settings_PaymentFeeRules_CRUD()
    {
        await Authenticate("settings-payment@test.com");

        var listRes = await Client.GetAsync("/api/settings/payment-fee-rules");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var createDto = new CreatePaymentFeeRuleDto(1, 3, 4.99m);
        var createRes = await Client.PostAsJsonAsync("/api/settings/payment-fee-rules", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await createRes.Content.ReadFromJsonAsync<PaymentFeeRuleDto>();

        var updateDto = new UpdatePaymentFeeRuleDto(1, 6, 5.49m);
        var updateRes = await Client.PutAsJsonAsync($"/api/settings/payment-fee-rules/{rule!.Id}", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteRes = await Client.DeleteAsync($"/api/settings/payment-fee-rules/{rule.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Settings_TaxProfile_GetAndUpdate()
    {
        await Authenticate("settings-tax@test.com");

        var getRes = await Client.GetAsync("/api/settings/tax-profile");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateDto = new UpdateTaxProfileDto("SimplesNacional", 6.0m, "SP");
        var updateRes = await Client.PutAsJsonAsync("/api/settings/tax-profile", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_AlertRules_CRUD()
    {
        await Authenticate("settings-alerts@test.com");

        var listRes = await Client.GetAsync("/api/settings/alert-rules");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var createDto = new CreateAlertRuleDto("StockLow", 10m, null);
        var createRes = await Client.PostAsJsonAsync("/api/settings/alert-rules", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var rule = await createRes.Content.ReadFromJsonAsync<AlertRuleDto>();

        var updateDto = new UpdateAlertRuleDto(5m, true);
        var updateRes = await Client.PutAsJsonAsync($"/api/settings/alert-rules/{rule!.Id}", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteRes = await Client.DeleteAsync($"/api/settings/alert-rules/{rule.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Settings_ResponseTimeSettings_GetAndUpdate()
    {
        await Authenticate("settings-response-time@test.com");

        var getRes = await Client.GetAsync("/api/settings/response-time-settings");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateDto = new UpdateResponseTimeSettingsDto(12, 6);
        var updateRes = await Client.PutAsJsonAsync("/api/settings/response-time-settings", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_NotificationPreferences_GetAndUpdate()
    {
        await Authenticate("settings-notif@test.com");

        var getRes = await Client.GetAsync("/api/settings/notification-preferences");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateDto = new List<UpdateNotificationPreferenceDto>
        {
            new("LowStock", true, true),
            new("NewSale", true, false)
        };
        var updateRes = await Client.PutAsJsonAsync("/api/settings/notification-preferences", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_Integrations_ReturnsOk()
    {
        await Authenticate("settings-integrations@test.com");
        var res = await Client.GetAsync("/api/settings/integrations");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_ReportSchedules_CRUD()
    {
        await Authenticate("settings-schedules@test.com");

        var listRes = await Client.GetAsync("/api/settings/report-schedules");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var createDto = new CreateReportScheduleDto("weekly", "admin@test.com", true);
        var createRes = await Client.PostAsJsonAsync("/api/settings/report-schedules", createDto);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var schedule = await createRes.Content.ReadFromJsonAsync<ReportScheduleDto>();

        var updateDto = new UpdateReportScheduleDto("monthly", "admin@test.com;finance@test.com", true);
        var updateRes = await Client.PutAsJsonAsync($"/api/settings/report-schedules/{schedule!.Id}", updateDto);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteRes = await Client.DeleteAsync($"/api/settings/report-schedules/{schedule.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

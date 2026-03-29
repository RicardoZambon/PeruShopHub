using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Email;

public static class EmailServiceExtensions
{
    /// <summary>
    /// Registers the email service. Uses ResendEmailService when EMAIL_API_KEY is configured,
    /// otherwise falls back to NoOpEmailService (logs to console).
    /// </summary>
    public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["EMAIL_API_KEY"];

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddHttpClient<IEmailService, ResendEmailService>(client =>
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            });
        }
        else
        {
            services.AddScoped<IEmailService, NoOpEmailService>();
        }

        return services;
    }
}

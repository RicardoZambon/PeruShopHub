using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Email;

public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Email not sent (no provider configured). To: {To}, Subject: {Subject}", to, subject);
        return Task.CompletedTask;
    }

    public Task SendAsync(IEnumerable<string> recipients, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Email not sent (no provider configured). To: {Recipients}, Subject: {Subject}",
            string.Join(", ", recipients), subject);
        return Task.CompletedTask;
    }
}

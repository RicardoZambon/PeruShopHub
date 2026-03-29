using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Email;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _fromAddress;

    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(9)
    ];

    public ResendEmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ResendEmailService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _fromAddress = configuration["EMAIL_FROM"] ?? "noreply@perushophub.com.br";
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        await SendWithRetryAsync([to], subject, htmlBody, textBody, ct);
    }

    public async Task SendAsync(IEnumerable<string> recipients, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        await SendWithRetryAsync(recipients.ToList(), subject, htmlBody, textBody, ct);
    }

    private async Task SendWithRetryAsync(List<string> recipients, string subject, string htmlBody, string? textBody, CancellationToken ct)
    {
        var payload = new ResendEmailPayload
        {
            From = _fromAddress,
            To = recipients,
            Subject = subject,
            Html = htmlBody,
            Text = textBody
        };

        for (var attempt = 0; attempt <= MaxRetries - 1; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("https://api.resend.com/emails", payload, ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent. To: {Recipients}, Subject: {Subject}",
                        string.Join(", ", recipients), subject);
                    return;
                }

                // Non-retryable client errors (except rate limiting)
                if (response.StatusCode is >= (HttpStatusCode)400 and < (HttpStatusCode)500
                    && response.StatusCode != HttpStatusCode.TooManyRequests)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Email send failed (non-retryable). Status: {Status}, To: {Recipients}, Subject: {Subject}, Error: {Error}",
                        response.StatusCode, string.Join(", ", recipients), subject, errorBody);
                    return;
                }

                // Retryable error
                _logger.LogWarning("Email send attempt {Attempt}/{MaxRetries} failed. Status: {Status}, To: {Recipients}, Subject: {Subject}",
                    attempt + 1, MaxRetries, response.StatusCode, string.Join(", ", recipients), subject);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Email send attempt {Attempt}/{MaxRetries} failed (network). To: {Recipients}, Subject: {Subject}",
                    attempt + 1, MaxRetries, string.Join(", ", recipients), subject);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Email send attempt {Attempt}/{MaxRetries} timed out. To: {Recipients}, Subject: {Subject}",
                    attempt + 1, MaxRetries, string.Join(", ", recipients), subject);
            }

            if (attempt < MaxRetries - 1)
            {
                await Task.Delay(RetryDelays[attempt], ct);
            }
        }

        _logger.LogError("Email send failed after {MaxRetries} attempts. To: {Recipients}, Subject: {Subject}",
            MaxRetries, string.Join(", ", recipients), subject);
    }
}

internal class ResendEmailPayload
{
    [JsonPropertyName("from")]
    public required string From { get; set; }

    [JsonPropertyName("to")]
    public required List<string> To { get; set; }

    [JsonPropertyName("subject")]
    public required string Subject { get; set; }

    [JsonPropertyName("html")]
    public required string Html { get; set; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }
}

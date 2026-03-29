namespace PeruShopHub.Core.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default);
    Task SendAsync(IEnumerable<string> recipients, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default);
}

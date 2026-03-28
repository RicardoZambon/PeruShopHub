namespace PeruShopHub.Application.Services;

public interface IIntegrationService
{
    Task<OAuthInitResult> InitiateOAuthAsync(string marketplaceId, CancellationToken ct = default);
    Task<OAuthCallbackResult> HandleOAuthCallbackAsync(string marketplaceId, string code, string state, CancellationToken ct = default);
    Task DisconnectAsync(string marketplaceId, CancellationToken ct = default);
}

public record OAuthInitResult(string AuthorizationUrl);
public record OAuthCallbackResult(string MarketplaceId, string SellerNickname, string Status, DateTime TokenExpiresAt);

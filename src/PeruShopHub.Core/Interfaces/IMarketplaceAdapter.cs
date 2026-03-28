namespace PeruShopHub.Core.Interfaces;

/// <summary>
/// Abstraction for marketplace API integration.
/// Each marketplace (ML, Amazon, Shopee) implements this interface
/// and is registered as a keyed DI service.
/// </summary>
public interface IMarketplaceAdapter
{
    /// <summary>Marketplace identifier, e.g. "mercadolivre", "amazon", "shopee".</summary>
    string MarketplaceId { get; }

    string GetAuthorizationUrl(string redirectUri, string state, string codeChallenge);
    Task<OAuthTokenResult> ExchangeCodeAsync(string code, string redirectUri, string codeVerifier, CancellationToken ct = default);
    Task<TokenResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<MarketplaceProduct> GetProductAsync(string externalId, CancellationToken ct = default);
    Task UpdateStockAsync(string externalId, int quantity, CancellationToken ct = default);
    Task<IReadOnlyList<MarketplaceOrder>> GetOrdersAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<MarketplaceOrderDetails> GetOrderDetailsAsync(string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<MarketplaceFee>> GetOrderFeesAsync(string orderId, CancellationToken ct = default);
}

// ── DTOs returned by IMarketplaceAdapter ────────────────────

public record TokenResult(string AccessToken, string RefreshToken, int ExpiresInSeconds);

public record OAuthTokenResult(string AccessToken, string RefreshToken, int ExpiresInSeconds, string UserId, string Nickname);

public record MarketplaceProduct(
    string ExternalId,
    string Title,
    string Status,
    decimal Price,
    string CurrencyId,
    int AvailableQuantity);

public record MarketplaceOrder(
    string ExternalOrderId,
    string Status,
    DateTimeOffset DateCreated,
    decimal TotalAmount);

public record MarketplaceOrderDetails(
    string ExternalOrderId,
    string Status,
    DateTimeOffset DateCreated,
    decimal TotalAmount,
    MarketplaceBuyer Buyer,
    IReadOnlyList<MarketplaceOrderItem> Items,
    MarketplaceShipping? Shipping);

public record MarketplaceBuyer(string ExternalId, string Nickname, string? Email);

public record MarketplaceOrderItem(
    string ExternalItemId,
    string Title,
    int Quantity,
    decimal UnitPrice);

public record MarketplaceShipping(
    string ExternalShippingId,
    string Status,
    decimal? ShippingCost);

public record MarketplaceFee(
    string Type,
    decimal Amount,
    string CurrencyId);

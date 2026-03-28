using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.Infrastructure.Marketplace;

/// <summary>
/// IMarketplaceAdapter implementation for Mercado Livre.
/// Uses HttpClientFactory named client "MercadoLivre" (base URL https://api.mercadolibre.com).
/// All API calls are logged with correlation ID, endpoint, status code, and elapsed time.
/// </summary>
public class MercadoLivreAdapter : IMarketplaceAdapter
{
    public string MarketplaceId => "mercadolivre";

    private readonly HttpClient _http;
    private readonly ILogger<MercadoLivreAdapter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public MercadoLivreAdapter(
        IHttpClientFactory httpClientFactory,
        ILogger<MercadoLivreAdapter> logger)
    {
        _http = httpClientFactory.CreateClient("MercadoLivre");
        _logger = logger;
    }

    // ── Token Refresh ────────────────────────────────────────

    public async Task<TokenResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var response = await SendAsync<MlTokenResponse>(
            HttpMethod.Post,
            "/oauth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            }),
            ct);

        return new TokenResult(response.AccessToken, response.RefreshToken, response.ExpiresIn);
    }

    // ── Products ─────────────────────────────────────────────

    public async Task<MarketplaceProduct> GetProductAsync(string externalId, CancellationToken ct = default)
    {
        var item = await SendAsync<MlItemResponse>(HttpMethod.Get, $"/items/{externalId}", null, ct);

        return new MarketplaceProduct(
            item.Id,
            item.Title,
            item.Status,
            item.Price,
            item.CurrencyId,
            item.AvailableQuantity);
    }

    public async Task UpdateStockAsync(string externalId, int quantity, CancellationToken ct = default)
    {
        var content = JsonContent.Create(new { available_quantity = quantity }, options: JsonOptions);
        await SendAsync(HttpMethod.Put, $"/items/{externalId}", content, ct);
    }

    // ── Orders ───────────────────────────────────────────────

    public async Task<IReadOnlyList<MarketplaceOrder>> GetOrdersAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var fromStr = from.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
        var toStr = to.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
        var url = $"/orders/search?seller=me&order.date_created.from={Uri.EscapeDataString(fromStr)}&order.date_created.to={Uri.EscapeDataString(toStr)}";

        var result = await SendAsync<MlOrderSearchResponse>(HttpMethod.Get, url, null, ct);

        return result.Results.Select(o => new MarketplaceOrder(
            o.Id.ToString(),
            o.Status,
            o.DateCreated,
            o.TotalAmount)).ToList();
    }

    public async Task<MarketplaceOrderDetails> GetOrderDetailsAsync(string orderId, CancellationToken ct = default)
    {
        var order = await SendAsync<MlOrderResponse>(HttpMethod.Get, $"/orders/{orderId}", null, ct);

        MarketplaceShipping? shipping = null;
        if (order.Shipping?.Id is not null)
        {
            try
            {
                var ship = await SendAsync<MlShippingResponse>(
                    HttpMethod.Get, $"/shipments/{order.Shipping.Id}", null, ct);

                shipping = new MarketplaceShipping(
                    ship.Id.ToString(),
                    ship.Status,
                    ship.ShippingOption?.Cost);
            }
            catch (MercadoLivreException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Shipping {ShippingId} not found for order {OrderId}", order.Shipping.Id, orderId);
            }
        }

        return new MarketplaceOrderDetails(
            order.Id.ToString(),
            order.Status,
            order.DateCreated,
            order.TotalAmount,
            new MarketplaceBuyer(
                order.Buyer?.Id.ToString() ?? string.Empty,
                order.Buyer?.Nickname ?? string.Empty,
                order.Buyer?.Email),
            order.OrderItems.Select(oi => new MarketplaceOrderItem(
                oi.Item.Id,
                oi.Item.Title,
                oi.Quantity,
                oi.UnitPrice)).ToList(),
            shipping);
    }

    public async Task<IReadOnlyList<MarketplaceFee>> GetOrderFeesAsync(string orderId, CancellationToken ct = default)
    {
        var billing = await SendAsync<MlBillingInfoResponse>(
            HttpMethod.Get, $"/orders/{orderId}/billing_info", null, ct);

        return billing.Detail.Select(d => new MarketplaceFee(
            d.Type,
            d.Amount,
            d.CurrencyId)).ToList();
    }

    // ── HTTP helpers ─────────────────────────────────────────

    private async Task<T> SendAsync<T>(HttpMethod method, string endpoint, HttpContent? content, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, endpoint, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new MercadoLivreException((int)response.StatusCode, null, $"Failed to deserialize response from {endpoint}");
    }

    private async Task SendAsync(HttpMethod method, string endpoint, HttpContent? content, CancellationToken ct)
    {
        using var _ = await SendCoreAsync(method, endpoint, content, ct);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method, string endpoint, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        if (content is not null)
            request.Content = content;

        var sw = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ML API request failed: {Method} {Endpoint} after {ElapsedMs}ms",
                method, endpoint, sw.ElapsedMilliseconds);
            throw;
        }
        sw.Stop();

        _logger.LogInformation(
            "ML API {Method} {Endpoint} → {StatusCode} in {ElapsedMs}ms",
            method, endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
        {
            MlErrorResponse? errorResponse = null;
            try
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                errorResponse = JsonSerializer.Deserialize<MlErrorResponse>(errorBody, JsonOptions);
            }
            catch
            {
                // Ignore deserialization failures for error body
            }

            var msg = errorResponse?.Message ?? $"ML API error: {(int)response.StatusCode}";
            _logger.LogWarning(
                "ML API error: {Method} {Endpoint} → {StatusCode}: {ErrorMessage}",
                method, endpoint, (int)response.StatusCode, msg);

            throw new MercadoLivreException((int)response.StatusCode, errorResponse, msg);
        }

        return response;
    }
}

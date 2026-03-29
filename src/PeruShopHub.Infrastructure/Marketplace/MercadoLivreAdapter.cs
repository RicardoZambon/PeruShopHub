using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
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
    private readonly string _clientId;
    private readonly string _clientSecret;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public MercadoLivreAdapter(
        IHttpClientFactory httpClientFactory,
        ILogger<MercadoLivreAdapter> logger,
        IConfiguration configuration)
    {
        _http = httpClientFactory.CreateClient("MercadoLivre");
        _logger = logger;
        _clientId = configuration["Marketplaces:MercadoLivre:ClientId"] ?? string.Empty;
        _clientSecret = configuration["Marketplaces:MercadoLivre:ClientSecret"] ?? string.Empty;
    }

    // ── OAuth ────────────────────────────────────────────────

    public string GetAuthorizationUrl(string redirectUri, string state, string codeChallenge)
    {
        return $"https://auth.mercadolivre.com.br/authorization" +
               $"?response_type=code" +
               $"&client_id={Uri.EscapeDataString(_clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
               $"&code_challenge_method=S256";
    }

    public async Task<OAuthTokenResult> ExchangeCodeAsync(
        string code, string redirectUri, string codeVerifier, CancellationToken ct = default)
    {
        var tokenResponse = await SendAsync<MlTokenResponse>(
            HttpMethod.Post,
            "/oauth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier
            }),
            ct);

        // Fetch user info to get user_id and nickname
        var userHttp = new HttpRequestMessage(HttpMethod.Get, "/users/me");
        userHttp.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        var userResponse = await _http.SendAsync(userHttp, ct);
        userResponse.EnsureSuccessStatusCode();
        var userBody = await userResponse.Content.ReadAsStringAsync(ct);
        var user = JsonSerializer.Deserialize<MlUserResponse>(userBody, JsonOptions)
            ?? throw new MercadoLivreException(500, null, "Failed to deserialize ML user info");

        return new OAuthTokenResult(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            tokenResponse.ExpiresIn,
            user.Id.ToString(),
            user.Nickname);
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

    public async Task<MarketplaceOrderSearchResult> SearchOrdersPagedAsync(
        DateTimeOffset from, DateTimeOffset to, int offset, int limit, CancellationToken ct = default)
    {
        var fromStr = from.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
        var toStr = to.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
        var url = $"/orders/search?seller=me" +
                  $"&order.date_created.from={Uri.EscapeDataString(fromStr)}" +
                  $"&order.date_created.to={Uri.EscapeDataString(toStr)}" +
                  $"&offset={offset}&limit={limit}" +
                  $"&sort=date_desc";

        var result = await SendAsync<MlOrderSearchResponse>(HttpMethod.Get, url, null, ct);

        var orders = result.Results.Select(o => new MarketplaceOrder(
            o.Id.ToString(),
            o.Status,
            o.DateCreated,
            o.TotalAmount)).ToList();

        var total = result.Paging?.Total ?? orders.Count;

        return new MarketplaceOrderSearchResult(orders, total, offset, limit);
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
        // Try real Billing Integration API first (actual charges)
        try
        {
            var fees = await GetBillingOrderDetailsAsync(orderId, ct);
            if (fees.Count > 0) return fees;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Billing integration API unavailable for order {OrderId}, falling back to billing_info", orderId);
        }

        // Fallback to /orders/{id}/billing_info
        var billing = await SendAsync<MlBillingInfoResponse>(
            HttpMethod.Get, $"/orders/{orderId}/billing_info", null, ct);

        return billing.Detail.Select(d => new MarketplaceFee(
            d.Type,
            d.Amount,
            d.CurrencyId)).ToList();
    }

    public async Task<IReadOnlyList<MarketplaceFee>> GetBillingOrderDetailsAsync(string orderId, CancellationToken ct = default)
    {
        var response = await SendAsync<MlBillingOrderDetailsResponse>(
            HttpMethod.Get,
            $"/billing/integration/group/ML/order/details?order_id={Uri.EscapeDataString(orderId)}",
            null, ct);

        return response.Results.Select(d => new MarketplaceFee(
            d.FeeType,
            d.FeeAmount,
            d.CurrencyId)).ToList();
    }

    // ── Item Search & Details ─────────────────────────────────

    public async Task<MarketplaceItemSearchResult> SearchSellerItemsAsync(
        string sellerId, string? scrollId, int limit = 50, CancellationToken ct = default)
    {
        var url = $"/users/{sellerId}/items/search?search_type=scan&limit={limit}";
        if (!string.IsNullOrEmpty(scrollId))
            url += $"&scroll_id={Uri.EscapeDataString(scrollId)}";

        var result = await SendAsync<MlItemSearchResponse>(HttpMethod.Get, url, null, ct);

        return new MarketplaceItemSearchResult(
            result.ScrollId,
            result.Results,
            result.Paging.Total);
    }

    public async Task<MarketplaceItemDetails> GetItemDetailsAsync(string externalId, CancellationToken ct = default)
    {
        var item = await SendAsync<MlItemFullResponse>(HttpMethod.Get, $"/items/{externalId}", null, ct);

        var pictures = item.Pictures.Select(p =>
            new MarketplaceItemPicture(p.Id, p.SecureUrl ?? p.Url)).ToList();

        var variations = item.Variations.Select(v =>
            new MarketplaceItemVariation(
                v.Id.ToString(),
                v.SellerCustomField,
                v.Price,
                v.AvailableQuantity,
                v.AttributeCombinations.ToDictionary(a => a.Name, a => a.ValueName ?? string.Empty),
                v.PictureIds
            )).ToList();

        // Map ML logistic_type to internal fulfillment type
        var fulfillmentType = item.Shipping?.LogisticType?.ToLowerInvariant() switch
        {
            "fulfillment" => "fulfillment",
            "cross_docking" => "cross_docking",
            "drop_off" => "drop_off",
            "xd_drop_off" => "xd_drop_off",
            _ => item.Shipping?.Mode?.ToLowerInvariant() == "me2" ? "self" : null
        };

        return new MarketplaceItemDetails(
            item.Id,
            item.Title,
            item.Status,
            item.Price,
            item.CurrencyId,
            item.AvailableQuantity,
            item.CategoryId,
            item.Permalink,
            item.Thumbnail,
            pictures,
            variations,
            fulfillmentType);
    }

    // ── Shipments ──────────────────────────────────────────────

    public async Task<MarketplaceShipmentDetails> GetShipmentDetailsAsync(string shipmentId, CancellationToken ct = default)
    {
        var ship = await SendAsync<MlShippingResponse>(HttpMethod.Get, $"/shipments/{shipmentId}", null, ct);

        var statusHistory = new List<MarketplaceShipmentEvent>();
        if (ship.StatusHistory is not null)
        {
            if (ship.StatusHistory.DateHandling.HasValue)
                statusHistory.Add(new MarketplaceShipmentEvent("handling", null, ship.StatusHistory.DateHandling.Value, "Em preparação"));
            if (ship.StatusHistory.DateShipped.HasValue)
                statusHistory.Add(new MarketplaceShipmentEvent("shipped", null, ship.StatusHistory.DateShipped.Value, "Enviado"));
            if (ship.StatusHistory.DateDelivered.HasValue)
                statusHistory.Add(new MarketplaceShipmentEvent("delivered", null, ship.StatusHistory.DateDelivered.Value, "Entregue"));
            if (ship.StatusHistory.DateReturned.HasValue)
                statusHistory.Add(new MarketplaceShipmentEvent("returned", null, ship.StatusHistory.DateReturned.Value, "Devolvido"));
            if (ship.StatusHistory.DateNotDelivered.HasValue)
                statusHistory.Add(new MarketplaceShipmentEvent("not_delivered", null, ship.StatusHistory.DateNotDelivered.Value, "Não entregue"));
            if (ship.StatusHistory.DateCancelled.HasValue)
                statusHistory.Add(new MarketplaceShipmentEvent("cancelled", null, ship.StatusHistory.DateCancelled.Value, "Cancelado"));
        }

        statusHistory.Sort((a, b) => a.Date.CompareTo(b.Date));

        return new MarketplaceShipmentDetails(
            ship.Id.ToString(),
            ship.Status,
            ship.TrackingNumber,
            ship.TrackingMethod?.Url,
            ship.TrackingMethod?.Name,
            ship.ShippingOption?.Name,
            ship.ShippingOption?.Cost,
            ship.DateCreated,
            ship.LastUpdated,
            ship.OrderId,
            statusHistory);
    }

    // ── Payments ────────────────────────────────────────────────

    public async Task<MarketplacePaymentDetails> GetPaymentDetailsAsync(string paymentId, CancellationToken ct = default)
    {
        var payment = await SendAsync<MlPaymentResponse>(HttpMethod.Get, $"/collections/{paymentId}", null, ct);

        return new MarketplacePaymentDetails(
            payment.Id.ToString(),
            payment.Status,
            payment.StatusDetail,
            payment.PaymentMethodId,
            payment.PaymentTypeId,
            payment.TransactionAmount,
            payment.ShippingAmount,
            payment.Installments,
            payment.CurrencyId,
            payment.DateCreated,
            payment.DateApproved,
            payment.Order?.Id);
    }

    // ── Fulfillment Stock ──────────────────────────────────────

    public async Task<MarketplaceFulfillmentStock> GetFulfillmentStockAsync(string inventoryId, CancellationToken ct = default)
    {
        var stock = await SendAsync<MlFulfillmentStockResponse>(
            HttpMethod.Get, $"/inventories/{inventoryId}/stock/fulfillment", null, ct);

        return new MarketplaceFulfillmentStock(
            inventoryId,
            stock.AvailableQuantity,
            stock.NotAvailableQuantity,
            stock.WarehouseId,
            stock.Status);
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

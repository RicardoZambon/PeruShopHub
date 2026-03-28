using System.Text.Json.Serialization;

namespace PeruShopHub.Infrastructure.Marketplace;

// ── ML API error response ───────────────────────────────────

public class MlErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("cause")]
    public List<MlErrorCause> Cause { get; set; } = [];
}

public class MlErrorCause
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// ── ML OAuth token response ─────────────────────────────────

public class MlTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}

// ── ML User (me) response ──────────────────────────────────

public class MlUserResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}

// ── ML Item (product) response ──────────────────────────────

public class MlItemResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency_id")]
    public string CurrencyId { get; set; } = string.Empty;

    [JsonPropertyName("available_quantity")]
    public int AvailableQuantity { get; set; }
}

// ── ML Order responses ──────────────────────────────────────

public class MlOrderSearchResponse
{
    [JsonPropertyName("results")]
    public List<MlOrderResponse> Results { get; set; } = [];

    [JsonPropertyName("paging")]
    public MlPaging? Paging { get; set; }
}

public class MlOrderResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("date_created")]
    public DateTimeOffset DateCreated { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("buyer")]
    public MlBuyer? Buyer { get; set; }

    [JsonPropertyName("order_items")]
    public List<MlOrderItem> OrderItems { get; set; } = [];

    [JsonPropertyName("shipping")]
    public MlShippingRef? Shipping { get; set; }
}

public class MlBuyer
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class MlOrderItem
{
    [JsonPropertyName("item")]
    public MlOrderItemDetail Item { get; set; } = new();

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }
}

public class MlOrderItemDetail
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}

public class MlShippingRef
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

// ── ML Shipping response ────────────────────────────────────

public class MlShippingResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("tracking_method")]
    public MlTrackingMethod? TrackingMethod { get; set; }

    [JsonPropertyName("date_created")]
    public DateTimeOffset? DateCreated { get; set; }

    [JsonPropertyName("last_updated")]
    public DateTimeOffset? LastUpdated { get; set; }

    [JsonPropertyName("order_id")]
    public long? OrderId { get; set; }

    [JsonPropertyName("shipping_option")]
    public MlShippingOption? ShippingOption { get; set; }

    [JsonPropertyName("status_history")]
    public MlShipmentStatusHistory? StatusHistory { get; set; }
}

public class MlShippingOption
{
    [JsonPropertyName("cost")]
    public decimal? Cost { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class MlTrackingMethod
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class MlShipmentStatusHistory
{
    [JsonPropertyName("date_handling")]
    public DateTimeOffset? DateHandling { get; set; }

    [JsonPropertyName("date_shipped")]
    public DateTimeOffset? DateShipped { get; set; }

    [JsonPropertyName("date_delivered")]
    public DateTimeOffset? DateDelivered { get; set; }

    [JsonPropertyName("date_returned")]
    public DateTimeOffset? DateReturned { get; set; }

    [JsonPropertyName("date_not_delivered")]
    public DateTimeOffset? DateNotDelivered { get; set; }

    [JsonPropertyName("date_cancelled")]
    public DateTimeOffset? DateCancelled { get; set; }
}

// ── ML Payment (collection) response ────────────────────────

public class MlPaymentResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("status_detail")]
    public string? StatusDetail { get; set; }

    [JsonPropertyName("payment_method_id")]
    public string? PaymentMethodId { get; set; }

    [JsonPropertyName("payment_type_id")]
    public string? PaymentTypeId { get; set; }

    [JsonPropertyName("transaction_amount")]
    public decimal TransactionAmount { get; set; }

    [JsonPropertyName("shipping_amount")]
    public decimal? ShippingAmount { get; set; }

    [JsonPropertyName("installments")]
    public int? Installments { get; set; }

    [JsonPropertyName("currency_id")]
    public string? CurrencyId { get; set; }

    [JsonPropertyName("date_created")]
    public DateTimeOffset DateCreated { get; set; }

    [JsonPropertyName("date_approved")]
    public DateTimeOffset? DateApproved { get; set; }

    [JsonPropertyName("order")]
    public MlPaymentOrderRef? Order { get; set; }
}

public class MlPaymentOrderRef
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

// ── ML Order billing info (fees) ────────────────────────────

public class MlBillingInfoResponse
{
    [JsonPropertyName("detail")]
    public List<MlBillingDetail> Detail { get; set; } = [];
}

public class MlBillingDetail
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency_id")]
    public string CurrencyId { get; set; } = string.Empty;
}

// ── ML Item search (scroll) response ───────────────────────

public class MlItemSearchResponse
{
    [JsonPropertyName("seller_id")]
    public string SellerId { get; set; } = string.Empty;

    [JsonPropertyName("results")]
    public List<string> Results { get; set; } = [];

    [JsonPropertyName("paging")]
    public MlPaging Paging { get; set; } = new();

    [JsonPropertyName("scroll_id")]
    public string? ScrollId { get; set; }
}

public class MlPaging
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

// ── ML Item full details (with variations & pictures) ──────

public class MlItemFullResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency_id")]
    public string CurrencyId { get; set; } = string.Empty;

    [JsonPropertyName("available_quantity")]
    public int AvailableQuantity { get; set; }

    [JsonPropertyName("category_id")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("permalink")]
    public string? Permalink { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("pictures")]
    public List<MlItemPicture> Pictures { get; set; } = [];

    [JsonPropertyName("variations")]
    public List<MlItemVariation> Variations { get; set; } = [];
}

public class MlItemPicture
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("secure_url")]
    public string? SecureUrl { get; set; }
}

public class MlItemVariation
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("available_quantity")]
    public int AvailableQuantity { get; set; }

    [JsonPropertyName("seller_custom_field")]
    public string? SellerCustomField { get; set; }

    [JsonPropertyName("attribute_combinations")]
    public List<MlAttributeCombination> AttributeCombinations { get; set; } = [];

    [JsonPropertyName("picture_ids")]
    public List<string> PictureIds { get; set; } = [];
}

public class MlAttributeCombination
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value_name")]
    public string? ValueName { get; set; }
}

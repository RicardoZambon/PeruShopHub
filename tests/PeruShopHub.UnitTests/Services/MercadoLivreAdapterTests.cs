using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PeruShopHub.Infrastructure.Marketplace;
using Xunit;

namespace PeruShopHub.UnitTests.Services;

public class MercadoLivreAdapterTests
{
    private readonly Mock<ILogger<MercadoLivreAdapter>> _logger = new();
    private readonly IConfiguration _config;
    private readonly string _clientId = "test-client-id";
    private readonly string _clientSecret = "test-client-secret";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public MercadoLivreAdapterTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Marketplaces:MercadoLivre:ClientId"] = _clientId,
                ["Marketplaces:MercadoLivre:ClientSecret"] = _clientSecret
            })
            .Build();
    }

    private (MercadoLivreAdapter adapter, Mock<HttpMessageHandler> handler) CreateAdapter(
        HttpStatusCode statusCode, object? responseBody)
    {
        var handler = new Mock<HttpMessageHandler>();
        var json = responseBody is not null ? JsonSerializer.Serialize(responseBody, JsonOptions) : "{}";

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.mercadolibre.com")
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("MercadoLivre")).Returns(httpClient);

        var adapter = new MercadoLivreAdapter(factory.Object, _logger.Object, _config);
        return (adapter, handler);
    }

    private (MercadoLivreAdapter adapter, Mock<HttpMessageHandler> handler) CreateAdapterWithSequence(
        params (HttpStatusCode status, object? body)[] responses)
    {
        var handler = new Mock<HttpMessageHandler>();
        var sequence = handler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var (status, body) in responses)
        {
            var json = body is not null ? JsonSerializer.Serialize(body, JsonOptions) : "{}";
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.mercadolibre.com")
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("MercadoLivre")).Returns(httpClient);

        var adapter = new MercadoLivreAdapter(factory.Object, _logger.Object, _config);
        return (adapter, handler);
    }

    // --- MarketplaceId ---

    [Fact]
    public void MarketplaceId_ReturnsMercadoLivre()
    {
        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, new { });
        adapter.MarketplaceId.Should().Be("mercadolivre");
    }

    // --- GetAuthorizationUrl ---

    [Fact]
    public void GetAuthorizationUrl_BuildsCorrectUrl()
    {
        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, new { });
        var url = adapter.GetAuthorizationUrl("https://callback.test/auth", "state123", "challenge456");

        url.Should().Contain("client_id=test-client-id");
        url.Should().Contain("redirect_uri=https%3A%2F%2Fcallback.test%2Fauth");
        url.Should().Contain("state=state123");
        url.Should().Contain("code_challenge=challenge456");
        url.Should().Contain("code_challenge_method=S256");
        url.Should().Contain("response_type=code");
    }

    // --- RefreshTokenAsync ---

    [Fact]
    public async Task RefreshToken_ReturnsTokenResult()
    {
        var tokenResponse = new
        {
            access_token = "new-access-token",
            refresh_token = "new-refresh-token",
            expires_in = 21600
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, tokenResponse);
        var result = await adapter.RefreshTokenAsync("old-refresh-token");

        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-refresh-token");
        result.ExpiresInSeconds.Should().Be(21600);
    }

    // --- GetProductAsync ---

    [Fact]
    public async Task GetProduct_ReturnsMarketplaceProduct()
    {
        var itemResponse = new
        {
            id = "MLB123",
            title = "Test Product",
            status = "active",
            price = 99.90m,
            currency_id = "BRL",
            available_quantity = 10
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, itemResponse);
        var result = await adapter.GetProductAsync("MLB123");

        result.ExternalId.Should().Be("MLB123");
        result.Title.Should().Be("Test Product");
        result.Status.Should().Be("active");
        result.Price.Should().Be(99.90m);
        result.AvailableQuantity.Should().Be(10);
    }

    // --- UpdateStockAsync ---

    [Fact]
    public async Task UpdateStock_SendsPutRequest()
    {
        var (adapter, handler) = CreateAdapter(HttpStatusCode.OK, new { });
        await adapter.UpdateStockAsync("MLB123", 50);

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Put &&
                r.RequestUri!.ToString().Contains("/items/MLB123")),
            ItExpr.IsAny<CancellationToken>());
    }

    // --- UpdateVariationStockAsync ---

    [Fact]
    public async Task UpdateVariationStock_SendsPutRequest()
    {
        var (adapter, handler) = CreateAdapter(HttpStatusCode.OK, new { });
        await adapter.UpdateVariationStockAsync("MLB123", "VAR456", 25);

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Put &&
                r.RequestUri!.ToString().Contains("/items/MLB123/variations/VAR456")),
            ItExpr.IsAny<CancellationToken>());
    }

    // --- GetOrdersAsync ---

    [Fact]
    public async Task GetOrders_ReturnsMarketplaceOrders()
    {
        var orderSearch = new
        {
            results = new[]
            {
                new { id = 1001L, status = "paid", date_created = DateTimeOffset.UtcNow, total_amount = 150m, buyer = (object?)null, order_items = Array.Empty<object>(), shipping = (object?)null },
                new { id = 1002L, status = "shipped", date_created = DateTimeOffset.UtcNow.AddHours(-1), total_amount = 200m, buyer = (object?)null, order_items = Array.Empty<object>(), shipping = (object?)null }
            },
            paging = new { total = 2, offset = 0, limit = 50 }
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, orderSearch);
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var result = await adapter.GetOrdersAsync(from, to);

        result.Should().HaveCount(2);
        result[0].ExternalOrderId.Should().Be("1001");
        result[0].TotalAmount.Should().Be(150m);
    }

    // --- SearchOrdersPagedAsync ---

    [Fact]
    public async Task SearchOrdersPaged_ReturnsTotalAndPaging()
    {
        var orderSearch = new
        {
            results = new[]
            {
                new { id = 2001L, status = "paid", date_created = DateTimeOffset.UtcNow, total_amount = 300m, buyer = (object?)null, order_items = Array.Empty<object>(), shipping = (object?)null }
            },
            paging = new { total = 50, offset = 0, limit = 10 }
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, orderSearch);
        var result = await adapter.SearchOrdersPagedAsync(
            DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, 0, 10);

        result.Total.Should().Be(50);
        result.Orders.Should().ContainSingle();
    }

    // --- GetItemDetailsAsync ---

    [Fact]
    public async Task GetItemDetails_MapsVariationsAndPictures()
    {
        var itemFull = new
        {
            id = "MLB999",
            title = "Full Item",
            status = "active",
            price = 150m,
            currency_id = "BRL",
            available_quantity = 5,
            category_id = "MLB1234",
            permalink = "https://mercadolivre.com/item/MLB999",
            thumbnail = "https://img.mercadolivre.com/thumb.jpg",
            pictures = new[]
            {
                new { id = "PIC-1", url = "http://img.com/1.jpg", secure_url = "https://img.com/1.jpg" },
                new { id = "PIC-2", url = "http://img.com/2.jpg", secure_url = (string?)null }
            },
            variations = new[]
            {
                new
                {
                    id = 10001L,
                    price = 150m,
                    available_quantity = 3,
                    seller_custom_field = "SKU-VAR-1",
                    attribute_combinations = new[] { new { id = "COLOR", name = "Cor", value_name = "Azul" } },
                    picture_ids = new[] { "PIC-1" }
                }
            },
            shipping = new { logistic_type = "fulfillment", mode = "me2", free_shipping = true }
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, itemFull);
        var result = await adapter.GetItemDetailsAsync("MLB999");

        result.ExternalId.Should().Be("MLB999");
        result.Pictures.Should().HaveCount(2);
        result.Pictures[0].Url.Should().Be("https://img.com/1.jpg"); // secure_url preferred
        result.Pictures[1].Url.Should().Be("http://img.com/2.jpg"); // fallback to url
        result.Variations.Should().ContainSingle();
        result.Variations[0].Sku.Should().Be("SKU-VAR-1");
        result.Variations[0].Attributes["Cor"].Should().Be("Azul");
        result.FulfillmentType.Should().Be("fulfillment");
    }

    [Theory]
    [InlineData("fulfillment", null, "fulfillment")]
    [InlineData("cross_docking", null, "cross_docking")]
    [InlineData("drop_off", null, "drop_off")]
    [InlineData("xd_drop_off", null, "xd_drop_off")]
    [InlineData(null, "me2", "self")]
    [InlineData(null, "not_specified", null)]
    public async Task GetItemDetails_MapsFulfillmentType(string? logisticType, string? mode, string? expected)
    {
        var itemFull = new
        {
            id = "MLB999",
            title = "Item",
            status = "active",
            price = 100m,
            currency_id = "BRL",
            available_quantity = 1,
            category_id = (string?)null,
            permalink = (string?)null,
            thumbnail = (string?)null,
            pictures = Array.Empty<object>(),
            variations = Array.Empty<object>(),
            shipping = new { logistic_type = logisticType, mode = mode, free_shipping = false }
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, itemFull);
        var result = await adapter.GetItemDetailsAsync("MLB999");

        result.FulfillmentType.Should().Be(expected);
    }

    // --- SearchSellerItemsAsync ---

    [Fact]
    public async Task SearchSellerItems_ReturnsScrollResult()
    {
        var searchResult = new
        {
            seller_id = "12345",
            results = new[] { "MLB001", "MLB002" },
            paging = new { total = 100, offset = 0, limit = 50 },
            scroll_id = "scroll_abc"
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, searchResult);
        var result = await adapter.SearchSellerItemsAsync("12345", null, 50);

        result.ScrollId.Should().Be("scroll_abc");
        result.ItemIds.Should().HaveCount(2);
        result.Total.Should().Be(100);
    }

    // --- GetShipmentDetailsAsync ---

    [Fact]
    public async Task GetShipmentDetails_MapsStatusHistory()
    {
        var shipment = new
        {
            id = 5001L,
            status = "delivered",
            tracking_number = "BR1234567890",
            tracking_method = new { name = "Correios", url = "https://tracking.test" },
            date_created = DateTimeOffset.UtcNow.AddDays(-3),
            last_updated = DateTimeOffset.UtcNow,
            order_id = 1001L,
            shipping_option = new { name = "Envio Normal", cost = 25.50m },
            status_history = new
            {
                date_handling = DateTimeOffset.UtcNow.AddDays(-3),
                date_shipped = DateTimeOffset.UtcNow.AddDays(-2),
                date_delivered = DateTimeOffset.UtcNow.AddDays(-1),
                date_returned = (DateTimeOffset?)null,
                date_not_delivered = (DateTimeOffset?)null,
                date_cancelled = (DateTimeOffset?)null
            }
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, shipment);
        var result = await adapter.GetShipmentDetailsAsync("5001");

        result.ExternalShipmentId.Should().Be("5001");
        result.Status.Should().Be("delivered");
        result.TrackingNumber.Should().Be("BR1234567890");
        result.Carrier.Should().Be("Correios");
        result.ShippingCost.Should().Be(25.50m);
        result.StatusHistory.Should().HaveCount(3); // handling, shipped, delivered
        result.StatusHistory[0].Status.Should().Be("handling");
    }

    // --- GetPaymentDetailsAsync ---

    [Fact]
    public async Task GetPaymentDetails_ReturnsPaymentInfo()
    {
        var payment = new
        {
            id = 7001L,
            status = "approved",
            status_detail = "accredited",
            payment_method_id = "pix",
            payment_type_id = "bank_transfer",
            transaction_amount = 299.90m,
            shipping_amount = 15m,
            installments = 1,
            currency_id = "BRL",
            date_created = DateTimeOffset.UtcNow.AddDays(-1),
            date_approved = DateTimeOffset.UtcNow.AddDays(-1),
            order = new { id = 1001L }
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, payment);
        var result = await adapter.GetPaymentDetailsAsync("7001");

        result.ExternalPaymentId.Should().Be("7001");
        result.Status.Should().Be("approved");
        result.PaymentMethodId.Should().Be("pix");
        result.TransactionAmount.Should().Be(299.90m);
        result.Installments.Should().Be(1);
    }

    // --- GetFulfillmentStockAsync ---

    [Fact]
    public async Task GetFulfillmentStock_ReturnsStockInfo()
    {
        var stock = new
        {
            available_quantity = 50,
            not_available_quantity = 5,
            warehouse_id = "WH-001",
            status = "available"
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, stock);
        var result = await adapter.GetFulfillmentStockAsync("INV-123");

        result.InventoryId.Should().Be("INV-123");
        result.AvailableQuantity.Should().Be(50);
        result.NotAvailableQuantity.Should().Be(5);
        result.WarehouseId.Should().Be("WH-001");
    }

    // --- SearchQuestionsAsync ---

    [Fact]
    public async Task SearchQuestions_ReturnsQuestions()
    {
        var questionSearch = new
        {
            total = 2,
            limit = 50,
            questions = new[]
            {
                new
                {
                    id = 3001L,
                    item_id = "MLB999",
                    text = "Is this available?",
                    status = "UNANSWERED",
                    date_created = DateTimeOffset.UtcNow.AddHours(-2),
                    answer = (object?)null,
                    from = new { id = 1L, nickname = "BUYER_123" }
                },
                new
                {
                    id = 3002L,
                    item_id = "MLB999",
                    text = "What color?",
                    status = "ANSWERED",
                    date_created = DateTimeOffset.UtcNow.AddHours(-5),
                    answer = new { text = "Blue", date_created = DateTimeOffset.UtcNow.AddHours(-4) } as object,
                    from = new { id = 2L, nickname = "BUYER_456" }
                }
            }
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, questionSearch);
        var result = await adapter.SearchQuestionsAsync("UNANSWERED", 0, 50);

        result.Total.Should().Be(2);
        result.Questions.Should().HaveCount(2);
        result.Questions[0].ExternalId.Should().Be("3001");
        result.Questions[0].QuestionText.Should().Be("Is this available?");
        result.Questions[0].AnswerText.Should().BeNull();
        result.Questions[1].AnswerText.Should().Be("Blue");
    }

    // --- PostAnswerAsync ---

    [Fact]
    public async Task PostAnswer_SendsPostRequest()
    {
        var (adapter, handler) = CreateAdapter(HttpStatusCode.OK, new { });
        await adapter.PostAnswerAsync("3001", "Yes, it is available!");

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString().Contains("/answers")),
            ItExpr.IsAny<CancellationToken>());
    }

    // --- Error handling ---

    [Fact]
    public async Task SendAsync_NonSuccessStatus_ThrowsMercadoLivreException()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(
                    JsonSerializer.Serialize(new { message = "Resource not found", error = "not_found", status = 404 }),
                    System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.mercadolibre.com")
        };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("MercadoLivre")).Returns(httpClient);

        var adapter = new MercadoLivreAdapter(factory.Object, _logger.Object, _config);
        var act = () => adapter.GetProductAsync("MLB-INVALID");

        var ex = await act.Should().ThrowAsync<MercadoLivreException>();
        ex.Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SendAsync_HttpException_PropagatesAndLogs()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.mercadolibre.com")
        };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("MercadoLivre")).Returns(httpClient);

        var adapter = new MercadoLivreAdapter(factory.Object, _logger.Object, _config);
        var act = () => adapter.GetProductAsync("MLB123");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // --- SearchClaimsAsync ---

    [Fact]
    public async Task SearchClaims_MapsClaimFields()
    {
        var claimSearch = new
        {
            data = new[]
            {
                new
                {
                    id = 9001L,
                    type = "claim",
                    status = "opened",
                    reason_id = "defective_product",
                    fulfilled = false,
                    date_created = DateTimeOffset.UtcNow.AddDays(-1),
                    last_updated = (DateTimeOffset?)null,
                    date_closed = (DateTimeOffset?)null,
                    resolution = (string?)null,
                    resource_id = 1001L as long?,
                    players = new[] { new { role = "complainant", user_id = 100L, nickname = "BUYER_X" } },
                    resource = new { id = "MLB999", description = "Broken item", quantity = 1, amount = 99.90m as decimal? }
                }
            },
            paging = new { total = 1, offset = 0, limit = 50 }
        };

        var (adapter, _) = CreateAdapter(HttpStatusCode.OK, claimSearch);
        var result = await adapter.SearchClaimsAsync("opened", 0, 50);

        result.Total.Should().Be(1);
        result.Claims.Should().ContainSingle();
        result.Claims[0].ExternalId.Should().Be("9001");
        result.Claims[0].Type.Should().Be("claim");
        result.Claims[0].BuyerNickname.Should().Be("BUYER_X");
        result.Claims[0].Reason.Should().Be("defective_product");
    }

    // --- GetOrderFeesAsync (billing fallback) ---

    [Fact]
    public async Task GetOrderFees_FallsToBillingInfo_WhenBillingApiEmpty()
    {
        var (adapter, _) = CreateAdapterWithSequence(
            (HttpStatusCode.OK, new { results = Array.Empty<object>() }),
            (HttpStatusCode.OK, new
            {
                detail = new[]
                {
                    new { type = "marketplace_fee", amount = 15m, currency_id = "BRL" },
                    new { type = "shipping_fee", amount = 10m, currency_id = "BRL" }
                }
            })
        );

        var result = await adapter.GetOrderFeesAsync("1001");

        result.Should().HaveCount(2);
        result[0].Type.Should().Be("marketplace_fee");
        result[0].Amount.Should().Be(15m);
    }
}

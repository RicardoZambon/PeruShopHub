using System.Text.Json.Serialization;

namespace PeruShopHub.Application.DTOs.Webhooks;

public class MercadoLivreWebhookDto
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("resource")]
    public string? Resource { get; set; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    [JsonPropertyName("application_id")]
    public long? ApplicationId { get; set; }

    [JsonPropertyName("sent")]
    public string? Sent { get; set; }

    [JsonPropertyName("attempts")]
    public int? Attempts { get; set; }

    [JsonPropertyName("received")]
    public string? Received { get; set; }
}

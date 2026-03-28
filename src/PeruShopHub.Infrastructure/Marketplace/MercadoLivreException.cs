namespace PeruShopHub.Infrastructure.Marketplace;

/// <summary>
/// Thrown when the Mercado Livre API returns a non-success status code.
/// Carries the deserialized error response when available.
/// </summary>
public class MercadoLivreException : Exception
{
    public int StatusCode { get; }
    public MlErrorResponse? ErrorResponse { get; }

    public MercadoLivreException(int statusCode, MlErrorResponse? errorResponse, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorResponse = errorResponse;
    }
}

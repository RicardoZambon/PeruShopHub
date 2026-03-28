using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace PeruShopHub.Infrastructure.Marketplace;

/// <summary>
/// Configures Polly v8 resilience pipeline for the MercadoLivre HTTP client.
/// Policy chain: Rate Limiter → Retry → Circuit Breaker (outer to inner).
/// </summary>
public static class MercadoLivreResilienceExtensions
{
    public static IHttpClientBuilder AddMercadoLivreResilience(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("MercadoLivreResilience", pipeline =>
        {
            // Rate Limiter: 300 requests/minute sliding window (outer-most)
            pipeline.AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            }));

            // Retry: 3 retries, exponential backoff, only 5xx/timeout (middle)
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                ShouldHandle = static args => ValueTask.FromResult(
                    args.Outcome.Result is { StatusCode: >= HttpStatusCode.InternalServerError }
                    || args.Outcome.Exception is HttpRequestException or TaskCanceledException)
            });

            // Circuit Breaker: opens after 5 failures, 30s break, then half-open (inner-most)
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = static args => ValueTask.FromResult(
                    args.Outcome.Result is { StatusCode: >= HttpStatusCode.InternalServerError }
                    || args.Outcome.Exception is HttpRequestException or TaskCanceledException)
            });
        });

        return builder;
    }
}

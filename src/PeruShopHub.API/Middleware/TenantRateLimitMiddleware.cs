using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;

namespace PeruShopHub.API.Middleware;

public class TenantRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantRateLimitMiddleware> _logger;

    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/health"
    };

    public TenantRateLimitMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<TenantRateLimitMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip rate limiting for unauthenticated endpoints, SignalR, and Swagger
        if (SkipPaths.Contains(path) || path.StartsWith("/hubs/") || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Super-admins are exempt from rate limiting
        var isSuperAdmin = user.FindFirstValue("is_super_admin") == "true";
        if (isSuperAdmin)
        {
            await _next(context);
            return;
        }

        // Extract tenant ID for rate limit key
        var tenantIdClaim = user.FindFirstValue("tenant_id");
        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            await _next(context);
            return;
        }

        var limit = _configuration.GetValue("RateLimiting:RequestsPerMinute", 100);
        var windowSeconds = 60;
        var now = DateTimeOffset.UtcNow;
        var windowKey = now.ToUnixTimeSeconds() / windowSeconds;
        var cacheKey = $"ratelimit:{tenantIdClaim}:{windowKey}";

        try
        {
            var countBytes = await cache.GetAsync(cacheKey, context.RequestAborted);
            var currentCount = countBytes is not null
                ? BitConverter.ToInt32(countBytes, 0)
                : 0;

            var remaining = Math.Max(0, limit - currentCount - 1);
            var resetTime = (windowKey + 1) * windowSeconds;

            // Set response headers regardless of whether limit is exceeded
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString();
                return Task.CompletedTask;
            });

            if (currentCount >= limit)
            {
                var retryAfter = resetTime - now.ToUnixTimeSeconds();
                context.Response.Headers["Retry-After"] = retryAfter.ToString();
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Limite de requisições excedido. Tente novamente em breve.",
                    retryAfterSeconds = retryAfter
                });

                _logger.LogWarning(
                    "Rate limit exceeded for tenant {TenantId}. Count: {Count}/{Limit}",
                    tenantIdClaim, currentCount, limit);

                return;
            }

            // Increment counter
            var newCount = currentCount + 1;
            var expiry = TimeSpan.FromSeconds((resetTime - now.ToUnixTimeSeconds()) + 1);
            await cache.SetAsync(cacheKey, BitConverter.GetBytes(newCount), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            }, context.RequestAborted);
        }
        catch (Exception ex)
        {
            // If Redis is down, allow the request through (fail-open)
            _logger.LogError(ex, "Rate limiting check failed for tenant {TenantId}. Allowing request.", tenantIdClaim);
        }

        await _next(context);
    }
}

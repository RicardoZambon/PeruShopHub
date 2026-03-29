using System.Collections.Concurrent;

namespace PeruShopHub.API.Middleware;

/// <summary>
/// IP-based rate limiting for authentication endpoints.
/// Prevents brute-force attacks on login, register, forgot-password, and reset-password.
/// </summary>
public class AuthRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthRateLimitMiddleware> _logger;
    private readonly int _maxAttempts;
    private readonly int _windowSeconds;

    // In-memory sliding window — keyed by IP + path
    private static readonly ConcurrentDictionary<string, List<DateTimeOffset>> _attempts = new();

    private static readonly HashSet<string> AuthPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/forgot-password",
        "/api/auth/reset-password"
    };

    public AuthRateLimitMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<AuthRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _maxAttempts = configuration.GetValue("RateLimiting:AuthMaxAttemptsPerMinute", 5);
        _windowSeconds = configuration.GetValue("RateLimiting:AuthWindowSeconds", 60);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (!AuthPaths.Contains(path) || context.Request.Method != "POST")
        {
            await _next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{ip}:{path}";
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddSeconds(-_windowSeconds);

        var timestamps = _attempts.GetOrAdd(key, _ => new List<DateTimeOffset>());

        lock (timestamps)
        {
            // Remove expired entries
            timestamps.RemoveAll(t => t < windowStart);

            if (timestamps.Count >= _maxAttempts)
            {
                var oldestInWindow = timestamps.Min();
                var retryAfter = (int)Math.Ceiling((oldestInWindow.AddSeconds(_windowSeconds) - now).TotalSeconds);
                retryAfter = Math.Max(1, retryAfter);

                context.Response.Headers["Retry-After"] = retryAfter.ToString();
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                _logger.LogWarning(
                    "Auth rate limit exceeded for IP {IP} on {Path}. Count: {Count}/{Limit}",
                    ip, path, timestamps.Count, _maxAttempts);

                context.Response.WriteAsJsonAsync(new
                {
                    error = "Muitas tentativas. Aguarde antes de tentar novamente.",
                    retryAfterSeconds = retryAfter
                }).GetAwaiter().GetResult();

                return;
            }

            timestamps.Add(now);
        }

        await _next(context);

        // Periodic cleanup of stale keys (every ~100 requests)
        if (Random.Shared.Next(100) == 0)
        {
            CleanupStaleEntries();
        }
    }

    private void CleanupStaleEntries()
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-_windowSeconds * 2);
        foreach (var kvp in _attempts)
        {
            lock (kvp.Value)
            {
                if (kvp.Value.Count == 0 || kvp.Value.Max() < cutoff)
                {
                    _attempts.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}

using System.Diagnostics;
using System.Security.Claims;
using PeruShopHub.Core.Interfaces;
using Serilog.Context;

namespace PeruShopHub.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly IConfiguration _configuration;

    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/health/ready",
        "/health/live",
        "/swagger",
    };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsLoggingEnabled() || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var tenantContext = context.RequestServices.GetService<ITenantContext>();
        var tenantId = tenantContext?.TenantId?.ToString();
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        using (LogContext.PushProperty("TenantId", tenantId ?? "anonymous"))
        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        using (LogContext.PushProperty("Endpoint", $"{context.Request.Method} {context.Request.Path}"))
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();

                using (LogContext.PushProperty("StatusCode", context.Response.StatusCode))
                using (LogContext.PushProperty("ElapsedMs", sw.ElapsedMilliseconds))
                {
                    var level = context.Response.StatusCode >= 500
                        ? LogLevel.Error
                        : context.Response.StatusCode >= 400
                            ? LogLevel.Warning
                            : LogLevel.Information;

                    _logger.Log(level,
                        "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method,
                        context.Request.Path.Value,
                        context.Response.StatusCode,
                        sw.ElapsedMilliseconds);
                }
            }
        }
    }

    private bool IsLoggingEnabled()
    {
        return _configuration.GetValue("Serilog:RequestLogging:Enabled", true);
    }

    private static bool ShouldSkip(PathString path)
    {
        var pathValue = path.Value ?? string.Empty;
        foreach (var skip in SkipPaths)
        {
            if (pathValue.StartsWith(skip, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return pathValue.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase);
    }
}

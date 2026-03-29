namespace PeruShopHub.API.Middleware;

/// <summary>
/// Adds OWASP-recommended security headers to all responses.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent MIME-type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking
            headers["X-Frame-Options"] = "DENY";

            // XSS protection (legacy browsers)
            headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer policy — send origin only on cross-origin
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions policy — restrict sensitive browser features
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // Content Security Policy
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "img-src 'self' data: https:; " +
                "connect-src 'self' ws: wss:; " +
                "frame-ancestors 'none';";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

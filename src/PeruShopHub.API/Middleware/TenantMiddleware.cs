using System.Security.Claims;
using PeruShopHub.Core.Interfaces;

namespace PeruShopHub.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/health"
    };

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip for unauthenticated endpoints
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

        var isSuperAdmin = user.FindFirstValue("is_super_admin") == "true";
        Guid? tenantId = null;

        if (isSuperAdmin)
        {
            // Super-admin can impersonate via header
            var headerTenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (Guid.TryParse(headerTenantId, out var headerGuid))
            {
                tenantId = headerGuid;
            }
            else
            {
                // Check JWT claim as fallback
                var claimTenantId = user.FindFirstValue("tenant_id");
                if (Guid.TryParse(claimTenantId, out var claimGuid))
                    tenantId = claimGuid;
            }
        }
        else
        {
            var claimTenantId = user.FindFirstValue("tenant_id");
            if (Guid.TryParse(claimTenantId, out var claimGuid))
            {
                tenantId = claimGuid;
            }

            // Non-super-admin must have a tenant on tenant-scoped endpoints
            if (tenantId is null && !path.StartsWith("/api/auth/"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Contexto de tenant ausente." });
                return;
            }
        }

        tenantContext.Set(tenantId, isSuperAdmin);
        await _next(context);
    }
}

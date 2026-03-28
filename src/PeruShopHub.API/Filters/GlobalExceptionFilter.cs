using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PeruShopHub.Application.Exceptions;
using Sentry;

namespace PeruShopHub.API.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case AppValidationException validationEx:
                context.Result = new BadRequestObjectResult(new { errors = validationEx.Errors });
                context.ExceptionHandled = true;
                break;

            case NotFoundException notFoundEx:
                context.Result = new NotFoundObjectResult(new { error = notFoundEx.Message });
                context.ExceptionHandled = true;
                break;

            case ConflictException conflictEx:
                context.Result = new ConflictObjectResult(new { error = conflictEx.Message });
                context.ExceptionHandled = true;
                break;

            case UnauthorizedAccessException:
                context.Result = new ObjectResult(new { error = "Acesso negado" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                context.ExceptionHandled = true;
                break;

            default:
                _logger.LogError(context.Exception, "Unhandled exception in {Controller}.{Action}",
                    context.RouteData.Values["controller"],
                    context.RouteData.Values["action"]);

                CaptureToSentry(context);

                context.Result = new ObjectResult(new { error = "Erro interno do servidor" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                context.ExceptionHandled = true;
                break;
        }
    }

    private static void CaptureToSentry(ExceptionContext context)
    {
        SentrySdk.ConfigureScope(scope =>
        {
            var httpContext = context.HttpContext;
            var user = httpContext.User;

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var tenantId = user.FindFirstValue("TenantId");

            scope.SetTag("controller", context.RouteData.Values["controller"]?.ToString() ?? "unknown");
            scope.SetTag("action", context.RouteData.Values["action"]?.ToString() ?? "unknown");

            if (!string.IsNullOrEmpty(userId))
            {
                scope.User = new SentryUser { Id = userId };
            }

            if (!string.IsNullOrEmpty(tenantId))
            {
                scope.SetTag("tenant_id", tenantId);
            }
        });

        SentrySdk.CaptureException(context.Exception);
    }
}

using Serilog.Context;

namespace Maliev.EmployeeService.Api.Middleware;

/// <summary>
/// Middleware to handle correlation IDs for request tracing
/// Phase 16 - T395: Structured logging with correlation IDs
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID from header or generate new one
        var correlationId = GetOrCreateCorrelationId(context);

        // Add correlation ID to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

        // Push correlation ID to Serilog LogContext for structured logging
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers.UserAgent.ToString()))
        using (LogContext.PushProperty("RemoteIpAddress", context.Connection.RemoteIpAddress?.ToString()))
        {
            // Add user information if authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                using (LogContext.PushProperty("UserId", context.User.Identity.Name))
                using (LogContext.PushProperty("UserRoles", string.Join(",", context.User.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value))))
                {
                    await _next(context);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from header
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Extension method to register CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}

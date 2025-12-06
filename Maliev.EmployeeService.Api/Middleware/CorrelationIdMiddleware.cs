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

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">The logger instance</param>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the HTTP request and adds correlation ID to the context
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID from header or generate new one
        var correlationId = GetOrCreateCorrelationId(context);

        // Add correlation ID to response headers
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

        // Add to HttpContext for downstream access
        context.Items["CorrelationId"] = correlationId;

        // Build scope properties for structured logging using standard .NET logging
        context.Request.Headers.TryGetValue("User-Agent", out var userAgent);

        var scopeProperties = new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.Value,
            ["RequestMethod"] = context.Request.Method,
            ["UserAgent"] = userAgent.ToString(),
            ["RemoteIpAddress"] = context.Connection.RemoteIpAddress?.ToString()
        };

        // Add user information if authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            scopeProperties["UserId"] = context.User.Identity.Name;
            var roles = context.User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value);
            scopeProperties["UserRoles"] = string.Join(",", roles);
        }

        using (_logger.BeginScope(scopeProperties))
        {
            _logger.LogDebug("CorrelationId set for request {RequestMethod} {RequestPath}: {CorrelationId}", context.Request.Method, context.Request.Path, correlationId);
            await _next(context);
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
        return Guid.NewGuid().ToString("D");
    }
}

/// <summary>
/// Extension methods for registering CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the CorrelationIdMiddleware to the application pipeline
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for method chaining</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}

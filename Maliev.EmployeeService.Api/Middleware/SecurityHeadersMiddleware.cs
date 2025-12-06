namespace Maliev.EmployeeService.Api.Middleware;

/// <summary>
/// Middleware to add security headers to all HTTP responses
/// Phase 16 - T384: Security Headers
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and adds security headers to the response
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Prevents MIME-sniffing attacks
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: Prevents clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection: Legacy XSS protection (still useful for older browsers)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer-Policy: Controls referrer information
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Content-Security-Policy: Prevents XSS, clickjacking, and other code injection attacks
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +  // Swagger requires unsafe-inline and unsafe-eval
            "style-src 'self' 'unsafe-inline'; " +  // Swagger requires unsafe-inline
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'");

        // Permissions-Policy: Controls which browser features can be used
        context.Response.Headers.Append("Permissions-Policy",
            "accelerometer=(), " +
            "camera=(), " +
            "geolocation=(), " +
            "gyroscope=(), " +
            "magnetometer=(), " +
            "microphone=(), " +
            "payment=(), " +
            "usb=()");

        // Remove server header to avoid information disclosure
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering SecurityHeadersMiddleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds the SecurityHeadersMiddleware to the application pipeline
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for method chaining</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

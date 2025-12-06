using System.Net;
using System.Text.Json;

namespace Maliev.EmployeeService.Api.Middleware;

/// <summary>
/// Global exception handling middleware that converts exceptions to standardized error responses
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="environment">The host environment</param>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Processes the HTTP request and handles any unhandled exceptions
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
        };

        context.Response.StatusCode = (int)statusCode;

        var errorResponse = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Details = _environment.IsDevelopment() ? exception.ToString() : null,
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}

/// <summary>
/// Represents a standardized error response
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional error details (only included in development)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the request path that generated the error
    /// </summary>
    public string Path { get; set; } = string.Empty;
}

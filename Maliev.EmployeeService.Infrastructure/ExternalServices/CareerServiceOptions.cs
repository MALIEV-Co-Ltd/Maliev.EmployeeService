namespace Maliev.EmployeeService.Infrastructure.ExternalServices;

/// <summary>
/// Configuration options for the Career Service external service client.
/// </summary>
public class CareerServiceOptions
{
    /// <summary>
    /// The base URL for the Career Service API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The timeout in seconds for HTTP requests to the Career Service.
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 30;
}

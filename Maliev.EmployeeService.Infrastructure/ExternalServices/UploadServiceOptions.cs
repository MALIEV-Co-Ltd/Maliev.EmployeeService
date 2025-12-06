namespace Maliev.EmployeeService.Infrastructure.ExternalServices;

/// <summary>
/// Configuration options for the Upload Service external service client.
/// </summary>
public class UploadServiceOptions
{
    /// <summary>
    /// The base URL for the Upload Service API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The timeout in seconds for HTTP requests to the Upload Service.
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 30;
}

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Maliev.EmployeeService.Api.HealthChecks;

/// <summary>
/// Health check that verifies Google Cloud Storage connectivity via Upload Service
/// Phase 16 - T398: Integration health checks
/// </summary>
public class GoogleCloudStorageHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCloudStorageHealthCheck> _logger;

    public GoogleCloudStorageHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GoogleCloudStorageHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the same configuration path as HttpClient registration in Program.cs
            // This will be populated from Google Secret Manager via ExternalServices__UploadService__BaseUrl
            var uploadServiceUrl = _configuration["ExternalServices:UploadService:BaseUrl"] ?? "http://localhost:8082";

            // Create HTTP client with timeout
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // Attempt to call the upload service health endpoint
            var healthEndpoint = $"{uploadServiceUrl.TrimEnd('/')}/health";
            var response = await httpClient.GetAsync(healthEndpoint, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy(
                    "Google Cloud Storage (via Upload Service) is healthy",
                    data: new Dictionary<string, object>
                    {
                        ["status"] = "healthy",
                        ["upload_service_status"] = (int)response.StatusCode,
                        ["upload_service_url"] = uploadServiceUrl
                    });
            }

            return HealthCheckResult.Degraded(
                $"Upload Service returned non-success status: {response.StatusCode}",
                data: new Dictionary<string, object>
                {
                    ["status"] = "degraded",
                    ["upload_service_status"] = (int)response.StatusCode,
                    ["upload_service_url"] = uploadServiceUrl
                });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Upload Service health check failed - service may be unavailable");

            return HealthCheckResult.Unhealthy(
                "Cannot reach Upload Service (Google Cloud Storage)",
                ex,
                data: new Dictionary<string, object>
                {
                    ["status"] = "unreachable",
                    ["error"] = ex.Message
                });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Upload Service health check timed out");

            return HealthCheckResult.Unhealthy(
                "Upload Service health check timed out",
                ex,
                data: new Dictionary<string, object>
                {
                    ["status"] = "timeout",
                    ["error"] = "Health check timed out after 5 seconds"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Cloud Storage health check failed");

            return HealthCheckResult.Unhealthy(
                "Google Cloud Storage health check failed with exception",
                ex,
                data: new Dictionary<string, object>
                {
                    ["status"] = "exception",
                    ["error"] = ex.Message
                });
        }
    }
}

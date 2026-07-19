using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Maliev.EmployeeService.Api.HealthChecks;

/// <summary>
/// Health check that verifies RabbitMQ connectivity via MassTransit
/// Phase 16 - T398: Integration health checks
/// </summary>
public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly IBusControl _busControl;
    private readonly ILogger<RabbitMQHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQHealthCheck"/> class
    /// </summary>
    /// <param name="busControl">The MassTransit bus control instance</param>
    /// <param name="_logger">The logger instance</param>
    public RabbitMQHealthCheck(IBusControl busControl, ILogger<RabbitMQHealthCheck> _logger)
    {
        _busControl = busControl;
        this._logger = _logger;
    }

    /// <summary>
    /// Checks the health of the RabbitMQ connection
    /// </summary>
    /// <param name="context">The health check context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the health check result</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if bus is started
            if (_busControl == null)
            {
                return HealthCheckResult.Unhealthy(
                    "RabbitMQ bus is not configured",
                    data: new Dictionary<string, object>
                    {
                        ["status"] = "not_configured"
                    });
            }

            // Get probe result to check bus health
            var probeResult = _busControl.GetProbeResult();

            // Check if we can read the probe result
            using var reader = new System.IO.StringReader(probeResult.ToString()!);
            var resultText = await reader.ReadToEndAsync(cancellationToken);

            // Simple check: if the result contains error indicators, mark as unhealthy
            if (resultText.Contains("fault", StringComparison.OrdinalIgnoreCase) ||
                resultText.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                resultText.Contains("exception", StringComparison.OrdinalIgnoreCase))
            {
                return HealthCheckResult.Unhealthy(
                    "RabbitMQ connection issues detected",
                    data: new Dictionary<string, object>
                    {
                        ["status"] = "unhealthy",
                        ["probe_result_length"] = resultText.Length
                    });
            }

            return HealthCheckResult.Healthy(
                "RabbitMQ connection is healthy",
                data: new Dictionary<string, object>
                {
                    ["status"] = "healthy",
                    ["probe_result_length"] = resultText.Length
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed");

            return HealthCheckResult.Unhealthy(
                "RabbitMQ health check failed with exception",
                ex,
                data: new Dictionary<string, object>
                {
                    ["status"] = "exception",
                    ["error"] = ex.Message
                });
        }
    }
}

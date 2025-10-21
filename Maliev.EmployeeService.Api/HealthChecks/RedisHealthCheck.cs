using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Maliev.EmployeeService.Api.HealthChecks;

/// <summary>
/// Health check that verifies Redis connectivity
/// Phase 16 - T398: Integration health checks
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IDistributedCache cache, ILogger<RedisHealthCheck> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to write and read from Redis
            var testKey = $"health_check_{Guid.NewGuid()}";
            var testValue = $"test_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var testBytes = System.Text.Encoding.UTF8.GetBytes(testValue);

            // Write test value
            await _cache.SetAsync(testKey, testBytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            }, cancellationToken);

            // Read test value back
            var retrievedBytes = await _cache.GetAsync(testKey, cancellationToken);

            if (retrievedBytes == null)
            {
                return HealthCheckResult.Unhealthy(
                    "Redis read operation returned null",
                    data: new Dictionary<string, object>
                    {
                        ["status"] = "read_failed",
                        ["test_key"] = testKey
                    });
            }

            var retrievedValue = System.Text.Encoding.UTF8.GetString(retrievedBytes);

            if (retrievedValue != testValue)
            {
                return HealthCheckResult.Unhealthy(
                    "Redis read/write mismatch",
                    data: new Dictionary<string, object>
                    {
                        ["status"] = "value_mismatch",
                        ["expected"] = testValue,
                        ["actual"] = retrievedValue
                    });
            }

            // Clean up test key
            await _cache.RemoveAsync(testKey, cancellationToken);

            return HealthCheckResult.Healthy(
                "Redis connection is healthy",
                data: new Dictionary<string, object>
                {
                    ["status"] = "healthy",
                    ["operation"] = "read_write_delete"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");

            return HealthCheckResult.Unhealthy(
                "Redis health check failed with exception",
                ex,
                data: new Dictionary<string, object>
                {
                    ["status"] = "exception",
                    ["error"] = ex.Message
                });
        }
    }
}

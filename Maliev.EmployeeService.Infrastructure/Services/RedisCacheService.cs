using System.Text.Json;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.Services;

/// <summary>
/// Redis-based distributed caching service implementation
/// Phase 16 - T385: Redis Distributed Cache
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            return null; // Fail gracefully - don't break the application if cache is unavailable
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(value, _jsonOptions);

            var options = new DistributedCacheEntryOptions();

            if (absoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            }

            if (slidingExpiration.HasValue)
            {
                options.SlidingExpiration = slidingExpiration.Value;
            }

            // Default expiration if none specified: 1 hour absolute
            if (!absoluteExpiration.HasValue && !slidingExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
            _logger.LogDebug("Cache set for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            // Fail gracefully - don't break the application if cache is unavailable
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            // Fail gracefully
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based removal is not directly supported by IDistributedCache
        // This would require accessing Redis directly via IConnectionMultiplexer
        // For now, log a warning that this feature requires direct Redis access
        _logger.LogWarning(
            "Pattern-based cache removal is not supported with IDistributedCache abstraction. " +
            "Pattern: {Pattern}. Consider using cache invalidation events instead.", pattern);

        await Task.CompletedTask;
    }
}

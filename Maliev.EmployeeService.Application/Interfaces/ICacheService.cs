namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Service for distributed caching operations
/// Phase 16 - T385: Redis Distributed Cache
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached value, or null if not found</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a value in the cache with the specified expiration
    /// </summary>
    /// <typeparam name="T">The type of the value to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="absoluteExpiration">Absolute expiration time (optional)</param>
    /// <param name="slidingExpiration">Sliding expiration time (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a cached value by key
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple cached values by pattern
    /// </summary>
    /// <param name="pattern">The key pattern (e.g., "employee:*")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}

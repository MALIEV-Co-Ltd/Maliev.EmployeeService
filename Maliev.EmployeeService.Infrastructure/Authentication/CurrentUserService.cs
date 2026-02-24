using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace Maliev.EmployeeService.Infrastructure.Authentication;

/// <summary>
/// Implementation of current user service using HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        IDistributedCache cache,
        ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    public Guid? PrincipalId
    {
        get
        {
            var subClaim = PrincipalIdentifier;

            if (string.IsNullOrEmpty(subClaim))
            {
                return null;
            }

            if (Guid.TryParse(subClaim, out var id))
            {
                return id;
            }

            // Handle service accounts or other non-GUID identities gracefully by returning null.
            // These identities can be checked via PrincipalIdentifier property.
            return null;
        }
    }

    public string? PrincipalIdentifier =>
        _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

    public async Task<Guid?> GetEmployeeIdAsync(CancellationToken cancellationToken = default)
    {
        var principalId = PrincipalId;
        if (principalId == null)
        {
            return null;
        }

        var cacheKey = $"principal_mapping:{principalId}";

        // Try get from cache
        var cachedBytes = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cachedBytes != null)
        {
            var cachedEmployeeId = JsonSerializer.Deserialize<EmployeeIdCacheWrapper>(cachedBytes);
            if (cachedEmployeeId != null)
            {
                return cachedEmployeeId.Id;
            }
        }

        // Lookup in DB via resolved repository
        using var scope = _serviceProvider.CreateScope();
        var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var employee = await employeeRepository.GetByPrincipalIdAsync(principalId.Value, cancellationToken);
        if (employee != null)
        {
            // Cache for 24 hours
            var wrapper = new EmployeeIdCacheWrapper(employee.Id);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(wrapper);
            await _cache.SetAsync(cacheKey, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            }, cancellationToken);
            return employee.Id;
        }

        return null;
    }

    public record EmployeeIdCacheWrapper(Guid Id);

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permission)
    {
        if (_httpContextAccessor.HttpContext?.User == null) return false;

        var userPermissions = _httpContextAccessor.HttpContext.User.Claims
            .Where(c => c.Type == "permissions" || c.Type == "permission" || c.Type == "role" || c.Type == "roles" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return PermissionMatcher.Match(permission, userPermissions);
    }
}

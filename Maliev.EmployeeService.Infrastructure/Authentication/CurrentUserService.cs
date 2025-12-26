using System.Security.Claims;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.Authentication;

/// <summary>
/// Implementation of current user service using HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        ICacheService cacheService,
        ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public Guid? PrincipalId
    {
        get
        {
            var subClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(subClaim))
            {
                return null;
            }

            if (Guid.TryParse(subClaim, out var id))
            {
                return id;
            }

            throw new UnauthorizedAccessException("Malformed principal identity in token.");
        }
    }

    public async Task<Guid?> GetEmployeeIdAsync(CancellationToken cancellationToken = default)
    {
        var principalId = PrincipalId;
        if (principalId == null)
        {
            return null;
        }

        var cacheKey = $"principal_mapping:{principalId}";
        
        // Try get from cache
        var cachedEmployeeId = await _cacheService.GetAsync<EmployeeIdCacheWrapper>(cacheKey, cancellationToken);
        if (cachedEmployeeId != null)
        {
            return cachedEmployeeId.Id;
        }

        // Lookup in DB via resolved repository
        using var scope = _serviceProvider.CreateScope();
        var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        
        var employee = await employeeRepository.GetByPrincipalIdAsync(principalId.Value, cancellationToken);
        if (employee != null)
        {
            // Cache for 24 hours (US3)
            await _cacheService.SetAsync(cacheKey, new EmployeeIdCacheWrapper(employee.Id), absoluteExpiration: TimeSpan.FromHours(24), cancellationToken: cancellationToken);
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
}

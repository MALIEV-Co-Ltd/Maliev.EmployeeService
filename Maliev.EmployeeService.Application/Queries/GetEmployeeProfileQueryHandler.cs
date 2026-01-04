using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Mapping;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.Aspire.ServiceDefaults.Caching;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetEmployeeProfileQuery
/// Phase 16 - T386: Implements distributed caching for frequently accessed employee profiles
/// </summary>
public class GetEmployeeProfileQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICacheService? _cacheService;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetEmployeeProfileQueryHandler> _logger;

    // Cache configuration
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);
    private const string CacheKeyPrefix = "employee:profile:";

    public GetEmployeeProfileQueryHandler(
        IEmployeeRepository employeeRepository,
        ILogger<GetEmployeeProfileQueryHandler> logger,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ICacheService? cacheService = null) // Optional to support environments without Redis
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<GetEmployeeProfileQueryResult> HandleAsync(
        GetEmployeeProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have ProfilesRead permission for this employee
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.EmployeeId}";
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ProfilesRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view this employee profile");
        }

        // Try to get from cache first
        var cacheKey = $"{CacheKeyPrefix}{query.EmployeeId}";

        if (_cacheService != null)
        {
            try
            {
                var cachedProfile = await _cacheService.GetAsync<EmployeeProfileDto>(cacheKey, cancellationToken);
                if (cachedProfile != null)
                {
                    _logger.LogDebug("Employee profile cache hit for {EmployeeId}", query.EmployeeId);
                    return new GetEmployeeProfileQueryResult(cachedProfile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving employee profile from cache for {EmployeeId}", query.EmployeeId);
                // Continue to database query
            }
        }

        var employee = await _employeeRepository.GetWithDetailsAsync(query.EmployeeId, cancellationToken);

        if (employee == null)
        {
            return new GetEmployeeProfileQueryResult(null);
        }

        // Map employee with emergency contacts
        var employeeWithContacts = await _employeeRepository.GetWithEmergencyContactsAsync(
            query.EmployeeId,
            cancellationToken);

        var profileDto = employee.ToEmployeeProfileDto(employeeWithContacts?.EmergencyContacts);

        // Cache the result
        if (_cacheService != null)
        {
            try
            {
                await _cacheService.SetAsync(
                    cacheKey,
                    profileDto,
                    ttl: CacheExpiration,
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Cached employee profile for {EmployeeId}", query.EmployeeId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching employee profile for {EmployeeId}", query.EmployeeId);
                // Don't fail the request if caching fails
            }
        }

        return new GetEmployeeProfileQueryResult(profileDto);
    }
}

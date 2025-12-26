using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetOrgChartQuery - builds hierarchical org chart
/// Phase 16 - T386: Implements distributed caching for org charts (expensive recursive queries)
/// </summary>
public class GetOrgChartQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICacheService? _cacheService;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetOrgChartQueryHandler> _logger;

    // Cache configuration - longer expiration for org charts since they change less frequently
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);
    private const string CacheKeyPrefix = "orgchart:";

    public GetOrgChartQueryHandler(
        IEmployeeRepository employeeRepository,
        ILogger<GetOrgChartQueryHandler> logger,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ICacheService? cacheService = null)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<GetOrgChartQueryResult> HandleAsync(
        GetOrgChartQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have ProfilesRead permission for this manager's org chart
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.ManagerId}/orgchart";
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ProfilesRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view this organization chart");
        }

        // Try to get from cache first
        var cacheKey = $"{CacheKeyPrefix}{query.ManagerId}:depth{query.MaxDepth}";

        if (_cacheService != null)
        {
            try
            {
                var cachedOrgChart = await _cacheService.GetAsync<OrgChartDto>(cacheKey, cancellationToken);
                if (cachedOrgChart != null)
                {
                    _logger.LogDebug("Org chart cache hit for manager {ManagerId} with depth {MaxDepth}",
                        query.ManagerId, query.MaxDepth);
                    return new GetOrgChartQueryResult(cachedOrgChart);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving org chart from cache for manager {ManagerId}",
                    query.ManagerId);
                // Continue to database query
            }
        }

        // Get the root manager
        var manager = await _employeeRepository.GetWithDetailsAsync(query.ManagerId, cancellationToken);

        if (manager == null)
        {
            return new GetOrgChartQueryResult(null);
        }

        // Build org chart recursively
        var orgChart = await BuildOrgChartAsync(manager, 0, query.MaxDepth, cancellationToken);

        // Cache the result
        if (_cacheService != null)
        {
            try
            {
                await _cacheService.SetAsync(
                    cacheKey,
                    orgChart,
                    absoluteExpiration: CacheExpiration,
                    cancellationToken: cancellationToken);

                _logger.LogDebug("Cached org chart for manager {ManagerId} with depth {MaxDepth}",
                    query.ManagerId, query.MaxDepth);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching org chart for manager {ManagerId}", query.ManagerId);
                // Don't fail the request if caching fails
            }
        }

        return new GetOrgChartQueryResult(orgChart);
    }

    private async Task<OrgChartDto> BuildOrgChartAsync(
        Employee employee,
        int currentLevel,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        var orgChartDto = new OrgChartDto
        {
            EmployeeId = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = employee.FullName,
            PreferredName = employee.PreferredName ?? string.Empty,
            JobTitle = employee.JobTitle ?? string.Empty,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            EmploymentStatus = employee.EmploymentStatus,
            WorkLocation = employee.WorkLocation ?? string.Empty,
            Level = currentLevel,
            DirectReports = new List<OrgChartDto>()
        };

        // Stop recursion if we've reached max depth
        if (currentLevel >= maxDepth)
        {
            // Still get count for display purposes
            var reportsForCount = await _employeeRepository.GetDirectReportsAsync(
                employee.Id,
                cancellationToken);
            orgChartDto.DirectReportsCount = reportsForCount.Count();
            orgChartDto.TotalReportsCount = orgChartDto.DirectReportsCount;
            return orgChartDto;
        }

        // Get direct reports
        var directReports = await _employeeRepository.GetDirectReportsAsync(
            employee.Id,
            cancellationToken);

        var directReportsList = directReports.ToList();
        orgChartDto.DirectReportsCount = directReportsList.Count;

        // Recursively build tree for each direct report
        int totalReportsCount = 0;
        foreach (var report in directReportsList)
        {
            var reportDto = await BuildOrgChartAsync(
                report,
                currentLevel + 1,
                maxDepth,
                cancellationToken);

            orgChartDto.DirectReports.Add(reportDto);
            totalReportsCount += 1 + reportDto.TotalReportsCount;
        }

        orgChartDto.TotalReportsCount = totalReportsCount;

        return orgChartDto;
    }
}

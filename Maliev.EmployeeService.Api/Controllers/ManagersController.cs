using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Manager team management and oversight (User Story 3).
/// Supports operations for viewing direct reports and organizational structures.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/managers")]
[Authorize]
public class ManagersController : ControllerBase
{
    private readonly GetTeamQueryHandler _getTeamHandler;
    private readonly GetOrgChartQueryHandler _getOrgChartHandler;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ManagersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagersController"/> class.
    /// </summary>
    /// <param name="getTeamHandler">The handler for getting team information.</param>
    /// <param name="getOrgChartHandler">The handler for getting organizational charts.</param>
    /// <param name="currentUserService">The current user service.</param>
    /// <param name="cache">The memory cache for storing expensive results.</param>
    /// <param name="logger">The logger instance.</param>
    public ManagersController(
        GetTeamQueryHandler getTeamHandler,
        GetOrgChartQueryHandler getOrgChartHandler,
        ICurrentUserService currentUserService,
        IMemoryCache cache,
        ILogger<ManagersController> logger)
    {
        _getTeamHandler = getTeamHandler;
        _getOrgChartHandler = getOrgChartHandler;
        _currentUserService = currentUserService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get direct reports for a manager with pagination.
    /// </summary>
    /// <param name="managerId">Manager's employee ID.</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of team members.</returns>
    [HttpGet("{managerId:guid}/direct-reports")]
    [RequirePermission(EmployeePermissions.ProfilesRead, ResourcePathTemplate = "employee/managers/{managerId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDirectReports(
        Guid managerId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1)
        {
            return BadRequest(new { message = "Page number must be greater than 0" });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { message = "Page size must be between 1 and 100" });
        }

        var query = new GetTeamQuery(managerId, pageNumber, pageSize);
        var result = await _getTeamHandler.HandleAsync(query, cancellationToken);

        return Ok(new
        {
            teamMembers = result.TeamMembers,
            pagination = new
            {
                totalCount = result.TotalCount,
                pageNumber = result.PageNumber,
                pageSize = result.PageSize,
                totalPages = result.TotalPages,
                hasNextPage = result.PageNumber < result.TotalPages,
                hasPreviousPage = result.PageNumber > 1
            }
        });
    }

    /// <summary>
    /// Get organizational chart starting from a manager.
    /// Returns hierarchical structure with configurable depth.
    /// Cached for 1 hour per manager and depth combination.
    /// </summary>
    /// <param name="managerId">Manager's employee ID.</param>
    /// <param name="depth">Maximum depth level (default: 3, max: 5).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Hierarchical org chart.</returns>
    [HttpGet("{managerId:guid}/org-chart")]
    [RequirePermission(EmployeePermissions.ProfilesRead, ResourcePathTemplate = "employee/managers/{managerId}")]
    [ProducesResponseType(typeof(OrgChartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrgChart(
        Guid managerId,
        [FromQuery] int depth = 3,
        CancellationToken cancellationToken = default)
    {
        // Validate depth parameter
        if (depth < 1 || depth > 5)
        {
            return BadRequest(new { message = "Depth must be between 1 and 5" });
        }

        // Check cache first
        var cacheKey = $"orgchart:{managerId}:{depth}";
        if (_cache.TryGetValue<OrgChartDto>(cacheKey, out var cachedOrgChart))
        {
            _logger.LogDebug("Org chart cache hit for {CacheKey}", cacheKey);
            return Ok(cachedOrgChart);
        }

        // Execute query
        var query = new GetOrgChartQuery(managerId, depth);
        var result = await _getOrgChartHandler.HandleAsync(query, cancellationToken);

        if (result.OrgChart == null)
        {
            return NotFound(new { message = "Manager not found" });
        }

        // Cache for 1 hour with sliding expiration
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1),
            Size = 1
        };
        _cache.Set(cacheKey, result.OrgChart, cacheOptions);

        _logger.LogInformation("Org chart retrieved for manager {ManagerId} with depth {Depth}",
            managerId, depth);

        return Ok(result.OrgChart);
    }
}

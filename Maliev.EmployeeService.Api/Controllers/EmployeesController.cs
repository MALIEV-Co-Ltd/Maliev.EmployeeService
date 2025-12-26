using Asp.Versioning;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.EmployeeService.Domain.Authorization;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// General employee management and lookups
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly GetEmployeeProfileQueryHandler _getProfileHandler;
    private readonly BusinessMetricsService _metricsService;
    private readonly ILogger<EmployeesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeesController"/> class
    /// </summary>
    public EmployeesController(
        IEmployeeRepository employeeRepository,
        GetEmployeeProfileQueryHandler getProfileHandler,
        BusinessMetricsService metricsService,
        ILogger<EmployeesController> logger)
    {
        _employeeRepository = employeeRepository;
        _getProfileHandler = getProfileHandler;
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Get employee profile by IAM Principal ID (US2)
    /// </summary>
    /// <param name="principalId">The IAM Principal ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Employee profile</returns>
    [HttpGet("by-principal/{principalId:guid}")]
    [RequirePermission(EmployeePermissions.ProfilesRead, ResourcePathTemplate = "employee/principals/{principalId}")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPrincipalId(Guid principalId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByPrincipalIdAsync(principalId, cancellationToken);

        if (employee == null)
        {
            _logger.LogWarning("Employee not found for principal {PrincipalId}", principalId);
            _metricsService.RecordPrincipalLookup(false);
            return NotFound(new { message = $"Employee not found for principal {principalId}" });
        }

        var query = new GetEmployeeProfileQuery(employee.Id);
        var result = await _getProfileHandler.HandleAsync(query, cancellationToken);

        _metricsService.RecordPrincipalLookup(true);
        return Ok(result.Profile);
    }
}

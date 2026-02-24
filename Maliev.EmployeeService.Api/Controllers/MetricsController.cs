using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Lightweight business metrics for dashboards.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/metrics")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<MetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsController"/> class.
    /// </summary>
    /// <param name="employeeRepository">The employee repository.</param>
    /// <param name="logger">The logger instance.</param>
    public MetricsController(IEmployeeRepository employeeRepository, ILogger<MetricsController> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get the total headcount of active employees.
    /// </summary>
    [HttpGet("headcount")]
    [RequirePermission(EmployeePermissions.ReportsView)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHeadcount(CancellationToken cancellationToken)
    {
        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var activeCount = allEmployees.Count(e => e.EmploymentStatus == EmploymentStatus.Active);

        return Ok(new { count = activeCount });
    }
}

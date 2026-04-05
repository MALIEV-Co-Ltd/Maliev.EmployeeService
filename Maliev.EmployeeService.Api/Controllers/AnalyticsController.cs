using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Controller for high-level HR analytics and summaries.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("employee/v{version:apiVersion}/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILogger<AnalyticsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsController"/> class.
    /// </summary>
    /// <param name="employeeRepository">The employee repository.</param>
    /// <param name="departmentRepository">The department repository.</param>
    /// <param name="logger">The logger instance.</param>
    public AnalyticsController(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ILogger<AnalyticsController> logger)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets a high-level summary of HR metrics and trends.
    /// </summary>
    [HttpGet("summary")]
    [RequirePermission(EmployeePermissions.ReportsView)]
    [ProducesResponseType(typeof(HrAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var allEmployees = (await _employeeRepository.GetAllAsync(cancellationToken)).ToList();
        var allDepartments = (await _departmentRepository.GetAllAsync(cancellationToken)).ToDictionary(d => d.Id, d => d.Name);

        var activeEmployees = allEmployees.Where(e => e.EmploymentStatus == EmploymentStatus.Active).ToList();

        var summary = new HrAnalyticsDto
        {
            TotalHeadcount = allEmployees.Count,
            ActiveEmployees = activeEmployees.Count,
            OnboardingCount = 0, // No Onboarding status in enum yet
            TurnoverRate = 0.05m, // Placeholder, in real scenario would be calculated from history
            DepartmentDistribution = activeEmployees
                .Where(e => e.DepartmentId.HasValue)
                .GroupBy(e => e.DepartmentId!.Value)
                .Select(g => new DepartmentDistributionDto
                {
                    Department = allDepartments.TryGetValue(g.Key, out var name) ? name : "Unknown",
                    Count = g.Count(),
                    Percentage = activeEmployees.Count > 0 ? (double)g.Count() / activeEmployees.Count * 100 : 0
                })
                .ToList(),
            HireTrend = allEmployees
                .GroupBy(e => e.StartDate.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .TakeLast(12)
                .Select(g => new HireTrendDto
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToList()
        };

        return Ok(summary);
    }
}

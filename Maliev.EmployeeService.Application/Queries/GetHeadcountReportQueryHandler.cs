using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Handler for GetHeadcountReportQuery
/// Aggregates employee headcount by department, employment type, tenure, and location
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class GetHeadcountReportQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    ///<summary>
    /// Initializes a new instance of the <see cref="GetHeadcountReportQueryHandler"/> class.
    /// </summary>
    /// <param name="employeeRepository">The employee repository.</param>
    /// <param name="iamClient">The IAM service client for authorization checks.</param>
    /// <param name="configuration">The configuration provider.</param>
    /// <param name="currentUserService">The service to access information about the current user.</param>
    public GetHeadcountReportQueryHandler(
        IEmployeeRepository employeeRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    ///<summary>
    /// Handles the query to generate headcount report
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A DTO containing the headcount report.</returns>
    public async Task<HeadcountReportDto> HandleAsync(
        GetHeadcountReportQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have ReportsView permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ReportsView, "employee/reports", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view headcount reports");
        }

        var asOfDate = query.AsOfDate ?? DateTime.UtcNow;

        // Get all active employees (optionally filtered by department)
        var employeesQuery = await _employeeRepository.GetAllAsync(cancellationToken);

        var employees = employeesQuery
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active)
            .Where(e => e.StartDate <= asOfDate)
            .Where(e => !e.TerminationDate.HasValue || e.TerminationDate.Value > asOfDate)
            .ToList();

        // Apply department filter if specified
        if (query.DepartmentId.HasValue)
        {
            employees = employees
                .Where(e => e.DepartmentId == query.DepartmentId.Value)
                .ToList();
        }

        var report = new HeadcountReportDto
        {
            TotalHeadcount = employees.Count,
            AsOfDate = asOfDate
        };

        // Group by department
        var departmentGroups = employees
            .Where(e => e.Department != null)
            .GroupBy(e => new { e.DepartmentId, DepartmentName = e.Department!.Name })
            .Select(g => new DepartmentHeadcountDto
            {
                DepartmentId = g.Key.DepartmentId!.Value,
                DepartmentName = g.Key.DepartmentName,
                Headcount = g.Count(),
                ManagerCount = g.Count(e => e.DirectReports != null && e.DirectReports.Any()),
                IndividualContributorCount = g.Count(e => e.DirectReports == null || !e.DirectReports.Any())
            })
            .OrderByDescending(d => d.Headcount)
            .ToList();

        report.ByDepartment = departmentGroups;

        // Group by employment type
        report.ByEmploymentType = employees
            .GroupBy(e => e.EmploymentType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by tenure band
        var tenureBands = employees
            .Select(e => new
            {
                Employee = e,
                TenureYears = (asOfDate - e.StartDate).TotalDays / 365.25
            })
            .GroupBy(x => GetTenureBand(x.TenureYears))
            .ToDictionary(g => g.Key, g => g.Count());

        report.ByTenureBand = tenureBands;

        // Group by location (using department location or work schedule location if available)
        // For now, we'll use a placeholder as location isn't directly on Employee entity
        // In a real system, this might come from WorkSchedule, Office, or Department
        report.ByLocation = new Dictionary<string, int>
        {
            { "Unknown", employees.Count }
        };

        return report;
    }

    ///<summary>
    /// Helper method to determine the tenure band for an employee.
    /// </summary>
    /// <param name="tenureYears">The tenure in years.</param>
    /// <returns>A string representing the tenure band.</returns>
    private static string GetTenureBand(double tenureYears)
    {
        return tenureYears switch
        {
            < 1 => "0-1 years",
            < 2 => "1-2 years",
            < 3 => "2-3 years",
            < 5 => "3-5 years",
            < 10 => "5-10 years",
            _ => "10+ years"
        };
    }
}

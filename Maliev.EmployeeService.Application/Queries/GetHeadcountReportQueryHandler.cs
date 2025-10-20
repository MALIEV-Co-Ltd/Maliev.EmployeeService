using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetHeadcountReportQuery
/// Aggregates employee headcount by department, employment type, tenure, and location
/// User Story 12 - Reporting & Analytics
/// </summary>
public class GetHeadcountReportQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetHeadcountReportQueryHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    /// <summary>
    /// Handles the query to generate headcount report
    /// </summary>
    public async Task<HeadcountReportDto> HandleAsync(
        GetHeadcountReportQuery query,
        CancellationToken cancellationToken = default)
    {
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

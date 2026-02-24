using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Handler for GetDiversityMetricsQuery
/// Analyzes employee demographics for diversity reporting
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class GetDiversityMetricsQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    ///<summary>
    /// Initializes a new instance of the <see cref="GetDiversityMetricsQueryHandler"/> class.
    /// </summary>
    /// <param name="employeeRepository">The employee repository.</param>
    /// <param name="iamClient">The IAM service client for authorization checks.</param>
    /// <param name="configuration">The configuration provider.</param>
    /// <param name="currentUserService">The service to access information about the current user.</param>
    public GetDiversityMetricsQueryHandler(
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
    /// Handles the query to generate diversity metrics
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A DTO containing the diversity metrics report.</returns>
    public async Task<DiversityMetricsDto> HandleAsync(
        GetDiversityMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have ReportsView permission
        var principalId = _currentUserService.PrincipalIdentifier;
        if (string.IsNullOrEmpty(principalId) ||
            (!await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ReportsView, "employee/reports/diversity", cancellationToken) &&
             !_currentUserService.HasPermission(EmployeePermissions.ReportsView)))
        {
            throw new UnauthorizedAccessException("You do not have permission to view diversity metrics reports");
        }

        var asOfDate = query.AsOfDate ?? DateTime.UtcNow;

        // Get all active employees
        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var employeesQuery = allEmployees
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active)
            .Where(e => e.StartDate <= asOfDate)
            .Where(e => !e.TerminationDate.HasValue || e.TerminationDate.Value > asOfDate);

        // Apply department filter if specified
        if (query.DepartmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e => e.DepartmentId == query.DepartmentId.Value);
        }

        var employees = employeesQuery.ToList();
        var totalHeadcount = employees.Count;

        var report = new DiversityMetricsDto
        {
            TotalHeadcount = totalHeadcount,
            AsOfDate = asOfDate
        };

        // Gender distribution
        // NOTE: Gender field not currently in Employee schema
        // This would require adding a Gender property to Employee entity
        report.ByGender = new Dictionary<string, int>
        {
            { "Not Available", totalHeadcount }
        };

        // Nationality distribution
        report.ByNationality = employees
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Nationality) ? "Not Specified" : e.Nationality)
            .ToDictionary(g => g.Key, g => g.Count())
            .OrderByDescending(kvp => kvp.Value)
            .Take(10) // Top 10 nationalities
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Age band distribution
        report.ByAgeBand = employees
            .Select(e => new { Employee = e, Age = CalculateAge(e.DateOfBirth, asOfDate) })
            .GroupBy(x => GetAgeBand(x.Age))
            .ToDictionary(g => g.Key, g => g.Count())
            .OrderBy(kvp => kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Department-level diversity metrics
        var departmentGroups = employees
            .Where(e => e.Department != null)
            .GroupBy(e => new { e.DepartmentId, DepartmentName = e.Department!.Name })
            .Select(g =>
            {
                var deptEmployees = g.ToList();

                return new DepartmentDiversityDto
                {
                    DepartmentId = g.Key.DepartmentId!.Value,
                    DepartmentName = g.Key.DepartmentName,
                    Headcount = deptEmployees.Count,
                    GenderDistribution = new Dictionary<string, int>
                    {
                        { "Not Available", deptEmployees.Count }
                    },
                    NationalityDistribution = deptEmployees
                        .GroupBy(e => string.IsNullOrWhiteSpace(e.Nationality) ? "Not Specified" : e.Nationality)
                        .ToDictionary(ng => ng.Key, ng => ng.Count())
                };
            })
            .OrderBy(d => d.DepartmentName)
            .ToList();

        report.ByDepartment = departmentGroups;

        // Leadership gender representation (managers)
        var managers = employees
            .Where(e => e.DirectReports != null && e.DirectReports.Any())
            .ToList();

        report.LeadershipByGender = new Dictionary<string, int>
        {
            { "Not Available", managers.Count }
        };

        return report;
    }

    ///<summary>
    /// Calculates the age of an employee based on their date of birth and an as-of date.
    /// </summary>
    /// <param name="dateOfBirth">The employee's date of birth.</param>
    /// <param name="asOfDate">The date as of which to calculate the age.</param>
    /// <returns>The calculated age in years.</returns>
    private static int CalculateAge(DateTime dateOfBirth, DateTime asOfDate)
    {
        var age = asOfDate.Year - dateOfBirth.Year;
        if (asOfDate.Month < dateOfBirth.Month ||
            (asOfDate.Month == dateOfBirth.Month && asOfDate.Day < dateOfBirth.Day))
        {
            age--;
        }
        return age;
    }

    ///<summary>
    /// Determines the age band for a given age.
    /// </summary>
    /// <param name="age">The age to categorize.</param>
    /// <returns>A string representing the age band.</returns>
    private static string GetAgeBand(int age)
    {
        return age switch
        {
            < 25 => "Under 25",
            < 30 => "25-29",
            < 35 => "30-34",
            < 40 => "35-39",
            < 45 => "40-44",
            < 50 => "45-49",
            < 55 => "50-54",
            < 60 => "55-59",
            _ => "60+"
        };
    }
}

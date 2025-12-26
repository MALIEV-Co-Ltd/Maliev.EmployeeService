using System.Globalization;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Handler for GetTurnoverAnalysisQuery
/// Calculates turnover rates and trends with voluntary/involuntary breakdown
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class GetTurnoverAnalysisQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    ///<summary>
    /// Initializes a new instance of the <see cref="GetTurnoverAnalysisQueryHandler"/> class.
    /// </summary>
    /// <param name="employeeRepository">The employee repository.</param>
    /// <param name="iamClient">The IAM service client for authorization checks.</param>
    /// <param name="configuration">The configuration provider.</param>
    /// <param name="currentUserService">The service to access information about the current user.</param>
    public GetTurnoverAnalysisQueryHandler(
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
    /// Handles the query to generate turnover analysis
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A DTO containing the turnover analysis report.</returns>
    public async Task<TurnoverAnalysisDto> HandleAsync(
        GetTurnoverAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have ReportsView permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ReportsView, "employee/reports", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view turnover analysis reports");
        }

        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var employeesList = allEmployees.ToList();

        // Get employees who were active at some point during the period
        var activeEmployees = employeesList
            .Where(e =>
                e.StartDate <= query.EndDate && // Hired before or during period
                (!e.TerminationDate.HasValue || e.TerminationDate.Value >= query.StartDate)) // Not terminated before period
            .ToList();

        // Filter by department if specified
        if (query.DepartmentId.HasValue)
        {
            activeEmployees = activeEmployees
                .Where(e => e.DepartmentId == query.DepartmentId.Value)
                .ToList();
        }

        // Get employees terminated during the period
        var terminatedEmployees = activeEmployees
            .Where(e => e.TerminationDate.HasValue &&
                       e.TerminationDate.Value >= query.StartDate &&
                       e.TerminationDate.Value <= query.EndDate)
            .ToList();

        // Calculate average headcount
        // Simple approach: (headcount at start + headcount at end) / 2
        var headcountAtStart = activeEmployees
            .Count(e => e.StartDate <= query.StartDate &&
                       (!e.TerminationDate.HasValue || e.TerminationDate.Value > query.StartDate));

        var headcountAtEnd = activeEmployees
            .Count(e => e.StartDate <= query.EndDate &&
                       (!e.TerminationDate.HasValue || e.TerminationDate.Value > query.EndDate));

        var averageHeadcount = (headcountAtStart + headcountAtEnd) / 2;

        // Categorize terminations (voluntary vs involuntary)
        // NOTE: The current schema doesn't include termination reason/type
        // In a real system, there would be a TerminationReason field
        // For now, we cannot distinguish between voluntary and involuntary terminations
        var voluntaryTerminations = 0; // Unknown - requires TerminationReason field
        var involuntaryTerminations = 0; // Unknown - requires TerminationReason field

        // Calculate rates
        var totalTerminations = terminatedEmployees.Count;
        var turnoverRate = averageHeadcount > 0
            ? (decimal)totalTerminations / averageHeadcount * 100
            : 0;

        var voluntaryRate = averageHeadcount > 0
            ? (decimal)voluntaryTerminations / averageHeadcount * 100
            : 0;

        var involuntaryRate = averageHeadcount > 0
            ? (decimal)involuntaryTerminations / averageHeadcount * 100
            : 0;

        var report = new TurnoverAnalysisDto
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            AverageHeadcount = averageHeadcount,
            TotalTerminations = totalTerminations,
            VoluntaryTerminations = voluntaryTerminations,
            InvoluntaryTerminations = involuntaryTerminations,
            TurnoverRate = Math.Round(turnoverRate, 2),
            VoluntaryTurnoverRate = Math.Round(voluntaryRate, 2),
            InvoluntaryTurnoverRate = Math.Round(involuntaryRate, 2)
        };

        // Department breakdown
        var departmentGroups = activeEmployees
            .Where(e => e.Department != null)
            .GroupBy(e => new { e.DepartmentId, DepartmentName = e.Department!.Name })
            .Select(g =>
            {
                var deptTerminations = terminatedEmployees.Count(e => e.DepartmentId == g.Key.DepartmentId);
                var deptHeadcount = g.Count();
                var deptRate = deptHeadcount > 0 ? (decimal)deptTerminations / deptHeadcount * 100 : 0;

                return new DepartmentTurnoverDto
                {
                    DepartmentId = g.Key.DepartmentId!.Value,
                    DepartmentName = g.Key.DepartmentName,
                    Headcount = deptHeadcount,
                    Terminations = deptTerminations,
                    TurnoverRate = Math.Round(deptRate, 2)
                };
            })
            .OrderByDescending(d => d.TurnoverRate)
            .ToList();

        report.ByDepartment = departmentGroups;

        // Monthly trend
        var monthlyTrend = new List<MonthlyTurnoverDto>();
        var currentDate = new DateTime(query.StartDate.Year, query.StartDate.Month, 1);
        var endMonth = new DateTime(query.EndDate.Year, query.EndDate.Month, 1);

        while (currentDate <= endMonth)
        {
            var monthStart = currentDate;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var monthTerminations = terminatedEmployees
                .Count(e => e.TerminationDate.HasValue &&
                           e.TerminationDate.Value >= monthStart &&
                           e.TerminationDate.Value <= monthEnd);

            var monthHeadcountStart = activeEmployees
                .Count(e => e.StartDate <= monthStart &&
                           (!e.TerminationDate.HasValue || e.TerminationDate.Value > monthStart));

            var monthHeadcountEnd = activeEmployees
                .Count(e => e.StartDate <= monthEnd &&
                           (!e.TerminationDate.HasValue || e.TerminationDate.Value > monthEnd));

            var monthAvgHeadcount = (monthHeadcountStart + monthHeadcountEnd) / 2;
            var monthRate = monthAvgHeadcount > 0
                ? (decimal)monthTerminations / monthAvgHeadcount * 100
                : 0;

            monthlyTrend.Add(new MonthlyTurnoverDto
            {
                Year = currentDate.Year,
                Month = currentDate.Month,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(currentDate.Month),
                Terminations = monthTerminations,
                AverageHeadcount = monthAvgHeadcount,
                TurnoverRate = Math.Round(monthRate, 2)
            });

            currentDate = currentDate.AddMonths(1);
        }

        report.MonthlyTrend = monthlyTrend;

        return report;
    }
}

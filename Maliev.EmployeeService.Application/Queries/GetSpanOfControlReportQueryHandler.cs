using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Handler for GetSpanOfControlReportQuery
/// Analyzes manager span of control against organizational limits
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class GetSpanOfControlReportQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetSpanOfControlReportQueryHandler(
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
    /// Handles the query to generate span of control report
    /// </summary>
    public async Task<SpanOfControlReportDto> HandleAsync(
        GetSpanOfControlReportQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have ReportsView permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ReportsView, "employee/reports/span-of-control", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view span of control reports");
        }

        // Get all active employees with their direct reports
        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var activeEmployees = allEmployees
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active)
            .ToList();

        // Apply department filter if specified
        if (query.DepartmentId.HasValue)
        {
            activeEmployees = activeEmployees
                .Where(e => e.DepartmentId == query.DepartmentId.Value)
                .ToList();
        }

        // Identify managers (employees with direct reports)
        var managers = activeEmployees
            .Where(e => e.DirectReports != null && e.DirectReports.Any(dr => dr.EmploymentStatus == EmploymentStatus.Active))
            .ToList();

        var report = new SpanOfControlReportDto
        {
            GeneratedDate = DateTime.UtcNow,
            TotalManagers = managers.Count
        };

        var managerSpans = new List<ManagerSpanDto>();

        foreach (var manager in managers)
        {
            // Count active direct reports only
            var activeDirectReports = manager.DirectReports
                .Where(dr => dr.EmploymentStatus == EmploymentStatus.Active)
                .ToList();

            var currentSpan = activeDirectReports.Count;

            // Determine if manager-of-managers
            var managerReportsCount = activeDirectReports.Count(dr =>
                dr.DirectReports != null && dr.DirectReports.Any(r => r.EmploymentStatus == EmploymentStatus.Active));
            var isManagerOfManagers = managerReportsCount > 0;

            // Determine max span (15 for IC managers, 8 for manager-of-managers)
            var maxSpan = isManagerOfManagers ? 8 : 15;

            // Calculate capacity percentage
            var capacityPercentage = maxSpan > 0 ? (decimal)currentSpan / maxSpan * 100 : 0;

            var managerSpan = new ManagerSpanDto
            {
                ManagerId = manager.Id,
                ManagerName = manager.LegalName?.FullName ?? "Unknown",
                ManagerTitle = manager.JobTitle,
                DepartmentId = manager.DepartmentId,
                DepartmentName = manager.Department?.Name,
                CurrentSpan = currentSpan,
                MaxSpan = maxSpan,
                CapacityPercentage = Math.Round(capacityPercentage, 1),
                IsManagerOfManagers = isManagerOfManagers,
                ManagerReportsCount = managerReportsCount
            };

            managerSpans.Add(managerSpan);
        }

        // Categorize managers
        report.ExceedingLimit = managerSpans
            .Where(m => m.CurrentSpan > m.MaxSpan)
            .OrderByDescending(m => m.CapacityPercentage)
            .ToList();

        report.ApproachingLimit = managerSpans
            .Where(m => m.CurrentSpan <= m.MaxSpan && m.CapacityPercentage >= 80)
            .OrderByDescending(m => m.CapacityPercentage)
            .ToList();

        report.WithinLimit = managerSpans
            .Where(m => m.CapacityPercentage < 80)
            .OrderBy(m => m.ManagerName)
            .ToList();

        // Filter by OnlyAtRisk if specified
        if (query.OnlyAtRisk)
        {
            report.WithinLimit.Clear();
        }

        // Calculate summary statistics
        if (managerSpans.Any())
        {
            var spans = managerSpans.Select(m => m.CurrentSpan).ToList();

            report.Summary = new SpanOfControlSummaryDto
            {
                ManagersExceedingLimit = report.ExceedingLimit.Count,
                ManagersApproachingLimit = report.ApproachingLimit.Count,
                ManagersWithinLimit = report.WithinLimit.Count,
                AverageSpan = Math.Round((decimal)spans.Average(), 1),
                MedianSpan = GetMedian(spans),
                MaxSpanFound = spans.Max()
            };
        }

        return report;
    }

    private static decimal GetMedian(List<int> values)
    {
        if (!values.Any()) return 0;

        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;

        if (count % 2 == 0)
        {
            // Even count: average of middle two
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2m;
        }
        else
        {
            // Odd count: middle value
            return sorted[count / 2];
        }
    }
}

using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Handler for GetLeaveUtilizationReportQuery
/// Analyzes leave accrual, usage, and carryover patterns
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class GetLeaveUtilizationReportQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    ///<summary>
    /// Initializes a new instance of the <see cref="GetLeaveUtilizationReportQueryHandler"/> class.
    /// </summary>
    /// <param name="employeeRepository">The employee repository.</param>
    /// <param name="leaveBalanceRepository">The leave balance repository.</param>
    /// <param name="iamClient">The IAM service client for authorization checks.</param>
    /// <param name="configuration">The configuration provider.</param>
    /// <param name="currentUserService">The service to access information about the current user.</param>
    public GetLeaveUtilizationReportQueryHandler(
        IEmployeeRepository employeeRepository,
        ILeaveBalanceRepository leaveBalanceRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    ///<summary>
    /// Handles the query to generate leave utilization report
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A DTO containing the leave utilization report.</returns>
    public async Task<LeaveUtilizationReportDto> HandleAsync(
        GetLeaveUtilizationReportQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have ReportsView permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ReportsView, "employee/reports/leave-utilization", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view leave utilization reports");
        }

        var year = query.Year ?? DateTime.UtcNow.Year;

        // Get all leave balances for the specified year
        var allBalances = await _leaveBalanceRepository.GetAllAsync(cancellationToken);
        var yearBalances = allBalances
            .Where(b => b.Year == year)
            .ToList();

        // Apply department filter if specified
        if (query.DepartmentId.HasValue)
        {
            yearBalances = yearBalances
                .Where(b => b.Employee != null && b.Employee.DepartmentId == query.DepartmentId.Value)
                .ToList();
        }

        // Get unique employees
        var employeeIds = yearBalances.Select(b => b.EmployeeId).Distinct().ToList();
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);
        var activeEmployees = employees
            .Where(e => employeeIds.Contains(e.Id) && e.EmploymentStatus == EmploymentStatus.Active)
            .ToList();

        var report = new LeaveUtilizationReportDto
        {
            Year = year,
            GeneratedDate = DateTime.UtcNow,
            TotalEmployees = activeEmployees.Count
        };

        // Overall utilization summary
        var totalEntitlement = yearBalances.Sum(b => b.TotalEntitlement);
        var totalUsed = yearBalances.Sum(b => b.UsedDays);
        var totalPending = yearBalances.Sum(b => b.PendingDays);
        var totalCarryForward = yearBalances.Sum(b => b.CarryForwardDays);
        var totalRemaining = yearBalances.Sum(b => b.RemainingDays);

        report.OverallUtilization = new LeaveUtilizationSummaryDto
        {
            TotalEntitlement = Math.Round(totalEntitlement, 2),
            TotalUsed = Math.Round(totalUsed, 2),
            TotalPending = Math.Round(totalPending, 2),
            TotalCarryForward = Math.Round(totalCarryForward, 2),
            TotalRemaining = Math.Round(totalRemaining, 2),
            UtilizationRate = totalEntitlement > 0 ? Math.Round(totalUsed / totalEntitlement * 100, 2) : 0,
            AverageDaysUsedPerEmployee = activeEmployees.Count > 0 ? Math.Round(totalUsed / activeEmployees.Count, 2) : 0,
            AverageDaysRemainingPerEmployee = activeEmployees.Count > 0 ? Math.Round(totalRemaining / activeEmployees.Count, 2) : 0
        };

        // By leave type
        var leaveTypeGroups = yearBalances
            .GroupBy(b => b.LeaveType)
            .Select(g =>
            {
                var typeEntitlement = g.Sum(b => b.TotalEntitlement);
                var typeUsed = g.Sum(b => b.UsedDays);
                var typePending = g.Sum(b => b.PendingDays);
                var typeRemaining = g.Sum(b => b.RemainingDays);
                var employeeCount = g.Select(b => b.EmployeeId).Distinct().Count();

                return new LeaveTypeUtilizationDto
                {
                    LeaveType = g.Key.ToString(),
                    EmployeeCount = employeeCount,
                    TotalEntitlement = Math.Round(typeEntitlement, 2),
                    TotalUsed = Math.Round(typeUsed, 2),
                    TotalPending = Math.Round(typePending, 2),
                    TotalRemaining = Math.Round(typeRemaining, 2),
                    UtilizationRate = typeEntitlement > 0 ? Math.Round(typeUsed / typeEntitlement * 100, 2) : 0,
                    AverageDaysUsedPerEmployee = employeeCount > 0 ? Math.Round(typeUsed / employeeCount, 2) : 0
                };
            })
            .OrderByDescending(t => t.TotalUsed)
            .ToList();

        report.ByLeaveType = leaveTypeGroups;

        // By department
        var departmentGroups = yearBalances
            .Where(b => b.Employee != null && b.Employee.Department != null)
            .GroupBy(b => new { b.Employee!.DepartmentId, DepartmentName = b.Employee.Department!.Name })
            .Select(g =>
            {
                var deptEntitlement = g.Sum(b => b.TotalEntitlement);
                var deptUsed = g.Sum(b => b.UsedDays);
                var deptRemaining = g.Sum(b => b.RemainingDays);
                var employeeCount = g.Select(b => b.EmployeeId).Distinct().Count();

                return new DepartmentLeaveUtilizationDto
                {
                    DepartmentId = g.Key.DepartmentId!.Value,
                    DepartmentName = g.Key.DepartmentName,
                    EmployeeCount = employeeCount,
                    TotalEntitlement = Math.Round(deptEntitlement, 2),
                    TotalUsed = Math.Round(deptUsed, 2),
                    TotalRemaining = Math.Round(deptRemaining, 2),
                    UtilizationRate = deptEntitlement > 0 ? Math.Round(deptUsed / deptEntitlement * 100, 2) : 0,
                    AverageDaysUsedPerEmployee = employeeCount > 0 ? Math.Round(deptUsed / employeeCount, 2) : 0
                };
            })
            .OrderByDescending(d => d.UtilizationRate)
            .ToList();

        report.ByDepartment = departmentGroups;

        // Employees at risk of losing leave (high balances approaching expiration)
        var today = DateTime.UtcNow;
        var atRisk = yearBalances
            .Where(b => b.RemainingDays > 5 && // More than 5 days remaining
                       b.ExpiryDate.HasValue &&
                       b.ExpiryDate.Value > today &&
                       (b.ExpiryDate.Value - today).TotalDays <= 90) // Expires within 90 days
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new EmployeeLeaveRiskDto
            {
                EmployeeId = b.EmployeeId,
                EmployeeName = b.Employee?.LegalName?.FullName ?? "Unknown",
                DepartmentName = b.Employee?.Department?.Name,
                LeaveType = b.LeaveType.ToString(),
                RemainingDays = Math.Round(b.RemainingDays, 2),
                ExpiryDate = b.ExpiryDate,
                DaysUntilExpiry = b.ExpiryDate.HasValue ? (int)(b.ExpiryDate.Value - today).TotalDays : 0
            })
            .Take(50) // Top 50 at-risk employees
            .ToList();

        report.AtRiskOfExpiration = atRisk;

        return report;
    }
}

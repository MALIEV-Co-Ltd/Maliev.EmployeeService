using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Prometheus;

namespace Maliev.EmployeeService.Application.Services;

/// <summary>
/// Service for calculating and exposing business KPI metrics
/// Constitution Principle X - Business Metrics requirement
/// Phase 15 - Business Metrics & Analytics
/// </summary>
public class BusinessMetricsService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;

    // Gauge metrics (current state)
    private static readonly Gauge ActiveEmployeeCount = Metrics.CreateGauge(
        "employee_active_count",
        "Total number of active employees",
        new GaugeConfiguration
        {
            LabelNames = new[] { "department", "employment_type" }
        });

    private static readonly Gauge EmployeeTurnoverRateMonthly = Metrics.CreateGauge(
        "employee_turnover_rate_monthly",
        "Monthly employee turnover rate (percentage)",
        new GaugeConfiguration
        {
            LabelNames = new[] { "turnover_type" } // voluntary, involuntary
        });

    private static readonly Gauge DepartmentHeadcountByName = Metrics.CreateGauge(
        "department_headcount_by_name",
        "Current headcount by department",
        new GaugeConfiguration
        {
            LabelNames = new[] { "department_name" }
        });

    private static readonly Gauge ProbationCompletionRate = Metrics.CreateGauge(
        "employee_probation_completion_rate",
        "Percentage of employees who successfully complete probation");

    private static readonly Gauge LeaveBalanceUtilizationRate = Metrics.CreateGauge(
        "leave_balance_utilization_rate",
        "Leave utilization rate (used / accrued)",
        new GaugeConfiguration
        {
            LabelNames = new[] { "leave_type" }
        });

    // Histogram metrics (distributions)
    private static readonly Histogram AverageOnboardingDurationDays = Metrics.CreateHistogram(
        "employee_onboarding_duration_days",
        "Time from hire date to active status (days)",
        new HistogramConfiguration
        {
            Buckets = new[] { 1.0, 3.0, 7.0, 14.0, 30.0, 60.0, 90.0 }
        });

    private static readonly Histogram LeaveRequestApprovalTimeHours = Metrics.CreateHistogram(
        "leave_request_approval_time_hours",
        "Time from leave request submission to approval/rejection (hours)",
        new HistogramConfiguration
        {
            Buckets = new[] { 1.0, 4.0, 8.0, 24.0, 48.0, 72.0, 168.0 } // 1h to 1 week
        });

    // Static constructor to initialize metrics with default values
    static BusinessMetricsService()
    {
        // Initialize gauge metrics with labeled variations so labels appear in output
        ActiveEmployeeCount.WithLabels("Unknown", "FullTime").Set(0);
        EmployeeTurnoverRateMonthly.WithLabels("total").Set(0);
        EmployeeTurnoverRateMonthly.WithLabels("voluntary").Set(0);
        EmployeeTurnoverRateMonthly.WithLabels("involuntary").Set(0);
        ProbationCompletionRate.Set(0);
        DepartmentHeadcountByName.WithLabels("Unknown").Set(0);
        LeaveBalanceUtilizationRate.WithLabels("Annual").Set(0);

        // Initialize at least one histogram observation to make the metric appear
        AverageOnboardingDurationDays.Observe(0);
        LeaveRequestApprovalTimeHours.Observe(0);
    }

    public BusinessMetricsService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveBalanceRepository leaveBalanceRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _leaveBalanceRepository = leaveBalanceRepository;
    }

    /// <summary>
    /// Calculate and update all business metrics
    /// Should be called periodically (e.g., every 5 minutes)
    /// </summary>
    public async Task UpdateAllMetricsAsync(CancellationToken cancellationToken = default)
    {
        await UpdateActiveEmployeeCountAsync(cancellationToken);
        await UpdateTurnoverRateAsync(cancellationToken);
        await UpdateDepartmentHeadcountAsync(cancellationToken);
        await UpdateProbationCompletionRateAsync(cancellationToken);
        await UpdateLeaveUtilizationRateAsync(cancellationToken);
        await UpdateOnboardingDurationAsync(cancellationToken);
        await UpdateLeaveApprovalTimeAsync(cancellationToken);
    }

    /// <summary>
    /// T418: Calculate active employee count by department and employment type
    /// </summary>
    public async Task UpdateActiveEmployeeCountAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);
        var activeEmployees = employees.Where(e => e.EmploymentStatus == EmploymentStatus.Active).ToList();

        // Reset all gauges
        ActiveEmployeeCount.Set(activeEmployees.Count);

        // Group by department and employment type
        var groups = activeEmployees
            .GroupBy(e => new
            {
                Department = e.Department?.Name ?? "Unknown",
                EmploymentType = e.EmploymentType.ToString()
            });

        foreach (var group in groups)
        {
            ActiveEmployeeCount
                .WithLabels(group.Key.Department, group.Key.EmploymentType)
                .Set(group.Count());
        }
    }

    /// <summary>
    /// T419: Calculate monthly turnover rate (voluntary vs involuntary)
    /// </summary>
    public async Task UpdateTurnoverRateAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);
        var allEmployees = employees.ToList();

        // Calculate for last 30 days
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var terminatedLastMonth = allEmployees
            .Where(e => e.EmploymentStatus == EmploymentStatus.Terminated &&
                       e.ModifiedDate.HasValue &&
                       e.ModifiedDate.Value >= thirtyDaysAgo)
            .ToList();

        var averageHeadcount = allEmployees.Count(e =>
            e.EmploymentStatus == EmploymentStatus.Active ||
            (e.EmploymentStatus == EmploymentStatus.Terminated &&
             e.ModifiedDate.HasValue &&
             e.ModifiedDate.Value >= thirtyDaysAgo));

        if (averageHeadcount > 0)
        {
            var totalTurnoverRate = (terminatedLastMonth.Count / (double)averageHeadcount) * 100;

            // For now, we'll track total turnover
            // In a real implementation, you'd track voluntary vs involuntary in the Employee entity
            EmployeeTurnoverRateMonthly.WithLabels("total").Set(totalTurnoverRate);

            // Placeholder for voluntary/involuntary breakdown
            // This would require additional fields in the Employee entity
            EmployeeTurnoverRateMonthly.WithLabels("voluntary").Set(0);
            EmployeeTurnoverRateMonthly.WithLabels("involuntary").Set(0);
        }
    }

    /// <summary>
    /// T422: Calculate headcount by department name
    /// </summary>
    public async Task UpdateDepartmentHeadcountAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);
        var activeEmployees = employees.Where(e => e.EmploymentStatus == EmploymentStatus.Active).ToList();

        var departmentGroups = activeEmployees
            .GroupBy(e => e.Department?.Name ?? "Unknown");

        foreach (var group in departmentGroups)
        {
            DepartmentHeadcountByName
                .WithLabels(group.Key)
                .Set(group.Count());
        }
    }

    /// <summary>
    /// T423: Calculate probation completion rate
    /// </summary>
    public async Task UpdateProbationCompletionRateAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);
        var allEmployees = employees.ToList();

        // Look at employees hired in the last 6 months
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var recentHires = allEmployees.Where(e => e.StartDate >= sixMonthsAgo).ToList();

        if (recentHires.Any())
        {
            // Assuming probation period is 90 days
            var probationEndDate = DateTime.UtcNow.AddDays(-90);

            var completedProbation = recentHires.Count(e =>
                e.StartDate <= probationEndDate &&
                e.EmploymentStatus == EmploymentStatus.Active);

            var eligibleForCompletion = recentHires.Count(e => e.StartDate <= probationEndDate);

            if (eligibleForCompletion > 0)
            {
                var completionRate = (completedProbation / (double)eligibleForCompletion) * 100;
                ProbationCompletionRate.Set(completionRate);
            }
        }
    }

    /// <summary>
    /// T424: Calculate leave balance utilization rate by leave type
    /// </summary>
    public async Task UpdateLeaveUtilizationRateAsync(CancellationToken cancellationToken = default)
    {
        var balances = await _leaveBalanceRepository.GetAllAsync(cancellationToken);

        var balanceGroups = balances.GroupBy(b => b.LeaveType);

        foreach (var group in balanceGroups)
        {
            var totalEntitlement = group.Sum(b => b.TotalEntitlement + b.CarryForwardDays);
            var totalUsed = group.Sum(b => b.UsedDays);

            if (totalEntitlement > 0)
            {
                var utilizationRate = (double)(totalUsed / totalEntitlement) * 100;
                LeaveBalanceUtilizationRate
                    .WithLabels(group.Key.ToString())
                    .Set(utilizationRate);
            }
        }
    }

    /// <summary>
    /// T420: Track onboarding duration (histogram)
    /// Call this when an employee status changes to Active
    /// </summary>
    public void RecordOnboardingDuration(DateTime hireDate, DateTime activeDate)
    {
        var durationDays = (activeDate - hireDate).TotalDays;
        if (durationDays >= 0)
        {
            AverageOnboardingDurationDays.Observe(durationDays);
        }
    }

    /// <summary>
    /// T421: Track leave approval time (histogram)
    /// Call this when a leave request is approved/rejected
    /// </summary>
    public void RecordLeaveApprovalTime(DateTime submittedDate, DateTime approvalDate)
    {
        var durationHours = (approvalDate - submittedDate).TotalHours;
        if (durationHours >= 0)
        {
            LeaveRequestApprovalTimeHours.Observe(durationHours);
        }
    }

    /// <summary>
    /// Update onboarding duration metrics from historical data
    /// </summary>
    private async Task UpdateOnboardingDurationAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);

        // For employees who recently became active, record their onboarding duration
        var recentlyActive = employees
            .Where(e => e.EmploymentStatus == EmploymentStatus.Active &&
                       e.ModifiedDate.HasValue &&
                       e.ModifiedDate.Value >= DateTime.UtcNow.AddDays(-30))
            .ToList();

        foreach (var employee in recentlyActive)
        {
            if (employee.ModifiedDate.HasValue)
            {
                RecordOnboardingDuration(employee.StartDate, employee.ModifiedDate.Value);
            }
        }
    }

    /// <summary>
    /// Update leave approval time metrics from historical data
    /// </summary>
    private async Task UpdateLeaveApprovalTimeAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _leaveRequestRepository.GetAllAsync(cancellationToken);

        // Get recently approved/denied requests
        var recentApprovals = requests
            .Where(r => (r.Status == LeaveRequestStatus.Approved || r.Status == LeaveRequestStatus.Denied) &&
                       r.ModifiedDate.HasValue &&
                       r.ModifiedDate.Value >= DateTime.UtcNow.AddDays(-30))
            .ToList();

        foreach (var request in recentApprovals)
        {
            if (request.ModifiedDate.HasValue)
            {
                RecordLeaveApprovalTime(request.CreatedDate, request.ModifiedDate.Value);
            }
        }
    }
}

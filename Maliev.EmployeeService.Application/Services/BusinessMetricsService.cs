using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;

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

    private static readonly Meter Meter = new("employees");

    // Data stores for Observable Gauges
    private static readonly ConcurrentDictionary<(string Department, string EmploymentType), int> _activeEmployeeCounts = new();
    private static readonly ConcurrentDictionary<string, double> _turnoverRates = new();
    private static readonly ConcurrentDictionary<string, int> _deptHeadcounts = new();
    private static double _probationCompletionRate = 0;
    private static readonly ConcurrentDictionary<string, double> _leaveUtilizationRates = new();

    // Histograms
    private static readonly Histogram<double> _onboardingDuration = Meter.CreateHistogram<double>(
        "employee_onboarding_duration_days",
        "days",
        "Time from hire date to active status");

    private static readonly Histogram<double> _leaveApprovalTime = Meter.CreateHistogram<double>(
        "leave_request_approval_time_hours",
        "hours",
        "Time from leave request submission to approval/rejection");

    // Static constructor to initialize metrics
    static BusinessMetricsService()
    {
        Meter.CreateObservableGauge("employee_active_count", () =>
            _activeEmployeeCounts.Select(kvp => new Measurement<int>(kvp.Value,
                new KeyValuePair<string, object?>("department", kvp.Key.Department),
                new KeyValuePair<string, object?>("employment_type", kvp.Key.EmploymentType))),
            description: "Total number of active employees");

        Meter.CreateObservableGauge("employee_turnover_rate_monthly", () =>
            _turnoverRates.Select(kvp => new Measurement<double>(kvp.Value,
                new KeyValuePair<string, object?>("turnover_type", kvp.Key))),
            description: "Monthly employee turnover rate (percentage)");

        Meter.CreateObservableGauge("department_headcount_by_name", () =>
            _deptHeadcounts.Select(kvp => new Measurement<int>(kvp.Value,
                new KeyValuePair<string, object?>("department_name", kvp.Key))),
            description: "Current headcount by department");

        Meter.CreateObservableGauge("employee_probation_completion_rate", () => _probationCompletionRate,
            description: "Percentage of employees who successfully complete probation");

        Meter.CreateObservableGauge("leave_balance_utilization_rate", () =>
            _leaveUtilizationRates.Select(kvp => new Measurement<double>(kvp.Value,
                new KeyValuePair<string, object?>("leave_type", kvp.Key))),
            description: "Leave utilization rate (used / accrued)");
            
        // Initialize default values
        _activeEmployeeCounts.TryAdd(("Unknown", "FullTime"), 0);
        _turnoverRates.TryAdd("total", 0);
        _turnoverRates.TryAdd("voluntary", 0);
        _turnoverRates.TryAdd("involuntary", 0);
        _deptHeadcounts.TryAdd("Unknown", 0);
        _leaveUtilizationRates.TryAdd("Annual", 0);
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

        // Clear existing counts but keep keys if possible? No, clear is safer to remove stale groups
        _activeEmployeeCounts.Clear();
        // Add total count
        // ActiveEmployeeCount.Set(activeEmployees.Count); // Original code had this but ObservableGauge works differently. 
        // We can add a "total" entry if needed, but usually sum by labels is enough.
        // The original code did: ActiveEmployeeCount.Set(activeEmployees.Count); AND then loops.
        // This implies the gauge had a value without labels? Prometheus allows this but OpenTelemetry ObservableGauge usually expects consistent labels.
        // We will stick to labeled metrics.

        // Group by department and employment type
        var groups = activeEmployees
            .GroupBy(e => new
            {
                Department = e.Department?.Name ?? "Unknown",
                EmploymentType = e.EmploymentType.ToString()
            });

        foreach (var group in groups)
        {
            _activeEmployeeCounts.TryAdd((group.Key.Department, group.Key.EmploymentType), group.Count());
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

            _turnoverRates["total"] = totalTurnoverRate;
            _turnoverRates["voluntary"] = 0; // Placeholder
            _turnoverRates["involuntary"] = 0; // Placeholder
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

        _deptHeadcounts.Clear();
        foreach (var group in departmentGroups)
        {
            _deptHeadcounts[group.Key] = group.Count();
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
                _probationCompletionRate = (completedProbation / (double)eligibleForCompletion) * 100;
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

        _leaveUtilizationRates.Clear();
        foreach (var group in balanceGroups)
        {
            var totalEntitlement = group.Sum(b => b.TotalEntitlement + b.CarryForwardDays);
            var totalUsed = group.Sum(b => b.UsedDays);

            if (totalEntitlement > 0)
            {
                var utilizationRate = (double)(totalUsed / totalEntitlement) * 100;
                _leaveUtilizationRates[group.Key.ToString()] = utilizationRate;
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
            _onboardingDuration.Record(durationDays);
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
            _leaveApprovalTime.Record(durationHours);
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

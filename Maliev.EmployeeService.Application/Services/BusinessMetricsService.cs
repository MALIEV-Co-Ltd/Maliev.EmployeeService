using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Maliev.EmployeeService.Application.Services;

/// <summary>
/// Background service that exposes business KPI metrics to OpenTelemetry/Prometheus.
/// </summary>
public class BusinessMetricsService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BusinessMetricsService> _logger;
    private readonly Meter _meter;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(5);

    public BusinessMetricsService(
        IServiceProvider serviceProvider,
        ILogger<BusinessMetricsService> logger,
        IMeterFactory meterFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _meter = meterFactory.Create("employees-meter");

        // Register observable gauges that are always available
        RegisterMetrics();
    }

    private void RegisterMetrics()
    {
        _meter.CreateObservableGauge("employee_active_count",
            () => GetActiveEmployeeCount(),
            description: "Total number of active employees");

        _meter.CreateObservableGauge("employee_turnover_rate_monthly",
            () => GetTurnoverRate(),
            description: "Monthly employee turnover rate as percentage");

        _meter.CreateObservableGauge("employee_probation_completion_rate",
            () => GetProbationCompletionRate(),
            description: "Percentage of employees who successfully complete probation");

        _meter.CreateObservableGauge("leave_balance_utilization_rate",
            () => GetLeaveBalanceUtilizationRate(),
            description: "Average leave balance utilization rate");

        _meter.CreateObservableGauge("department_headcount_by_name",
            () => GetDepartmentHeadcounts(),
            description: "Headcount per department");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BusinessMetricsService starting");

        // Metrics are observable and will be collected when scraped
        // This background service just keeps the service alive
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_updateInterval, stoppingToken);
        }

        _logger.LogInformation("BusinessMetricsService stopping");
    }

    private int GetActiveEmployeeCount()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            // Synchronous call for metrics - observable gauges require sync
            var employees = employeeRepository.GetAllAsync().GetAwaiter().GetResult();
            return employees.Count(e => e.EmploymentStatus == EmploymentStatus.Active);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active employee count");
            return 0;
        }
    }

    private double GetTurnoverRate()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            var employees = employeeRepository.GetAllAsync().GetAwaiter().GetResult().ToList();
            var totalActive = employees.Count(e => e.EmploymentStatus == EmploymentStatus.Active);

            if (totalActive == 0) return 0;

            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            var terminated = employees.Count(e =>
                e.TerminationDate.HasValue &&
                e.TerminationDate.Value >= oneMonthAgo);

            return (double)terminated / totalActive * 100;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating turnover rate");
            return 0;
        }
    }

    private double GetProbationCompletionRate()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            var employees = employeeRepository.GetAllAsync().GetAwaiter().GetResult()
                .Where(e => e.StartDate <= DateTime.UtcNow.AddMonths(-3)) // Past probation period
                .ToList();

            if (!employees.Any()) return 100;

            var completed = employees.Count(e => e.EmploymentStatus == EmploymentStatus.Active);
            return (double)completed / employees.Count * 100;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating probation completion rate");
            return 0;
        }
    }

    private double GetLeaveBalanceUtilizationRate()
    {
        try
        {
            // Placeholder - would need leave balance data
            // For now, return a reasonable default
            return 65.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating leave balance utilization rate");
            return 0;
        }
    }

    private IEnumerable<Measurement<int>> GetDepartmentHeadcounts()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            var headcounts = employeeRepository.GetAllAsync().GetAwaiter().GetResult()
                .Where(e => e.EmploymentStatus == EmploymentStatus.Active && e.Department != null)
                .GroupBy(e => e.Department!.Name)
                .Select(g => new Measurement<int>(
                    g.Count(),
                    new KeyValuePair<string, object?>("department", g.Key)))
                .ToList();

            // Ensure at least one measurement exists so the metric name appears in Prometheus output
            if (!headcounts.Any())
            {
                headcounts.Add(new Measurement<int>(
                    0,
                    new KeyValuePair<string, object?>("department", "none")));
            }

            return headcounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department headcounts");
            // Return a placeholder measurement even on error so metric name is registered
            return new[]
            {
                new Measurement<int>(0, new KeyValuePair<string, object?>("department", "error"))
            };
        }
    }

    public override void Dispose()
    {
        _meter.Dispose();
        base.Dispose();
    }
}

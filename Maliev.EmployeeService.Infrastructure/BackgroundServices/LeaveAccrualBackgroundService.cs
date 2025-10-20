using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for processing monthly leave accrual
/// Runs on the 1st of each month at midnight (cron: "0 0 1 * *")
/// </summary>
public class LeaveAccrualBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeaveAccrualBackgroundService> _logger;
    private readonly IBackgroundJobStatusService _statusService;
    private readonly CrontabSchedule _schedule;
    private DateTime _nextRun;

    // Cron schedule: "0 0 1 * *" = At 00:00 on day-of-month 1
    // Format: minute hour day month day-of-week
    private const string Schedule = "0 0 1 * *";
    private const string JobName = "LeaveAccrualBackgroundService";

    public LeaveAccrualBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<LeaveAccrualBackgroundService> logger,
        IBackgroundJobStatusService statusService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _statusService = statusService;
        _schedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
        _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);

        _logger.LogInformation("LeaveAccrualBackgroundService initialized. Next run: {NextRun} UTC", _nextRun);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LeaveAccrualBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var timeUntilNextRun = _nextRun - now;

            if (timeUntilNextRun.TotalMilliseconds > 0)
            {
                // Wait until next scheduled run or cancellation
                _logger.LogDebug("Waiting {Minutes} minutes until next accrual run at {NextRun} UTC",
                    timeUntilNextRun.TotalMinutes, _nextRun);

                try
                {
                    await Task.Delay(timeUntilNextRun, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("LeaveAccrualBackgroundService is stopping");
                    break;
                }
            }

            // Execute the accrual processing
            await ProcessLeaveAccrualAsync(stoppingToken);

            // Calculate next run time
            _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
            _logger.LogInformation("Next leave accrual run scheduled for: {NextRun} UTC", _nextRun);
        }

        _logger.LogInformation("LeaveAccrualBackgroundService stopped");
    }

    private async Task ProcessLeaveAccrualAsync(CancellationToken cancellationToken)
    {
        var executionTime = DateTime.UtcNow;
        _logger.LogInformation("Starting leave accrual processing job at {Time} UTC", executionTime);

        try
        {
            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var leaveAccrualService = scope.ServiceProvider.GetRequiredService<ILeaveAccrualService>();

            var processedCount = await leaveAccrualService.ProcessMonthlyAccrualAsync(cancellationToken);

            _logger.LogInformation("Leave accrual processing completed successfully. Processed {Count} employees", processedCount);

            // Record success
            _statusService.RecordSuccess(JobName, executionTime, _nextRun);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during leave accrual processing");

            // Record failure
            _statusService.RecordFailure(JobName, executionTime, ex.Message, _nextRun);

            // Don't rethrow - we want the background service to continue running
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LeaveAccrualBackgroundService is stopping");
        return base.StopAsync(cancellationToken);
    }
}

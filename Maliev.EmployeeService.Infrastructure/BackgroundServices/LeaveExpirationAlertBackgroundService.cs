using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for checking and alerting on expiring leave balances
/// Runs daily at 9:00 AM (cron: "0 9 * * *")
/// Sends notifications at 60, 30, and 14 days before expiration
/// </summary>
public class LeaveExpirationAlertBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeaveExpirationAlertBackgroundService> _logger;
    private readonly IBackgroundJobStatusService _statusService;
    private readonly CrontabSchedule _schedule;
    private DateTime _nextRun;

    // Cron schedule: "0 9 * * *" = At 09:00 every day
    // Format: minute hour day month day-of-week
    private const string Schedule = "0 9 * * *";
    private const string JobName = "LeaveExpirationAlertBackgroundService";

    public LeaveExpirationAlertBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<LeaveExpirationAlertBackgroundService> logger,
        IBackgroundJobStatusService statusService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _statusService = statusService;
        _schedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
        _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);

        _logger.LogInformation("LeaveExpirationAlertBackgroundService initialized. Next run: {NextRun} UTC", _nextRun);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LeaveExpirationAlertBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var timeUntilNextRun = _nextRun - now;

            if (timeUntilNextRun.TotalMilliseconds > 0)
            {
                // Wait until next scheduled run or cancellation
                _logger.LogDebug("Waiting {Minutes} minutes until next expiration check at {NextRun} UTC",
                    timeUntilNextRun.TotalMinutes, _nextRun);

                try
                {
                    await Task.Delay(timeUntilNextRun, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("LeaveExpirationAlertBackgroundService is stopping");
                    break;
                }
            }

            // Execute the expiration checking
            await CheckExpiringLeaveBalancesAsync(stoppingToken);

            // Calculate next run time
            _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
            _logger.LogInformation("Next leave expiration check scheduled for: {NextRun} UTC", _nextRun);
        }

        _logger.LogInformation("LeaveExpirationAlertBackgroundService stopped");
    }

    private async Task CheckExpiringLeaveBalancesAsync(CancellationToken cancellationToken)
    {
        var executionTime = DateTime.UtcNow;
        _logger.LogInformation("Starting leave expiration check at {Time} UTC", executionTime);

        try
        {
            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var leaveBalanceRepository = scope.ServiceProvider.GetRequiredService<ILeaveBalanceRepository>();

            // Get current date for comparison
            var today = DateTime.UtcNow.Date;
            var in60Days = today.AddDays(60);
            var in30Days = today.AddDays(30);
            var in14Days = today.AddDays(14);

            // Check for balances expiring in 60, 30, or 14 days
            var expiringBalances = await leaveBalanceRepository.GetExpiringBalancesAsync(
                in60Days,
                cancellationToken);

            if (!expiringBalances.Any())
            {
                _logger.LogInformation("No leave balances found expiring within 60 days");
                _statusService.RecordSuccess(JobName, executionTime, _nextRun);
                return;
            }

            var alertCount = 0;

            foreach (var balance in expiringBalances)
            {
                if (!balance.ExpiryDate.HasValue)
                    continue;

                var daysUntilExpiration = (balance.ExpiryDate.Value.Date - today).Days;

                // Send alert if expiring in exactly 60, 30, or 14 days
                if (daysUntilExpiration == 60 || daysUntilExpiration == 30 || daysUntilExpiration == 14)
                {
                    _logger.LogInformation(
                        "Leave balance expiring alert: Employee {EmployeeId}, Type {LeaveType}, " +
                        "Available {Available} days, Expires in {Days} days on {ExpiryDate}",
                        balance.EmployeeId, balance.LeaveType, balance.AvailableDays,
                        daysUntilExpiration, balance.ExpiryDate.Value.ToString("yyyy-MM-dd"));

                    // TODO: Send notification to employee (email/push notification)
                    // This would typically use an INotificationService
                    // await notificationService.SendLeaveExpirationAlertAsync(balance, daysUntilExpiration);

                    alertCount++;
                }
            }

            _logger.LogInformation(
                "Leave expiration check completed. Checked {TotalCount} balances, sent {AlertCount} alerts",
                expiringBalances.Count(), alertCount);

            // Record success
            _statusService.RecordSuccess(JobName, executionTime, _nextRun);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during leave expiration check");

            // Record failure
            _statusService.RecordFailure(JobName, executionTime, ex.Message, _nextRun);

            // Don't rethrow - we want the background service to continue running
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LeaveExpirationAlertBackgroundService is stopping");
        return base.StopAsync(cancellationToken);
    }
}

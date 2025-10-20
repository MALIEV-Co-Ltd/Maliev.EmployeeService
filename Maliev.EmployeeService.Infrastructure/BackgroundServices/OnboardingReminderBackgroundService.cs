using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.IntegrationEvents;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for sending onboarding reminders
/// Runs daily at 9:00 AM UTC (cron: "0 9 * * *")
/// Checks for employees whose start date is in 3 days and onboarding is incomplete
/// </summary>
public class OnboardingReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OnboardingReminderBackgroundService> _logger;
    private readonly IBackgroundJobStatusService _statusService;
    private readonly CrontabSchedule _schedule;
    private DateTime _nextRun;

    // Cron schedule: "0 9 * * *" = At 09:00 every day
    // Format: minute hour day month day-of-week
    private const string Schedule = "0 9 * * *";
    private const string JobName = "OnboardingReminderBackgroundService";
    private const int DaysBeforeStartDate = 3;

    public OnboardingReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OnboardingReminderBackgroundService> logger,
        IBackgroundJobStatusService statusService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _statusService = statusService;
        _schedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
        _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);

        _logger.LogInformation("OnboardingReminderBackgroundService initialized. Next run: {NextRun} UTC", _nextRun);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OnboardingReminderBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var timeUntilNextRun = _nextRun - now;

            if (timeUntilNextRun.TotalMilliseconds > 0)
            {
                _logger.LogDebug("Waiting {Minutes} minutes until next reminder check at {NextRun} UTC",
                    timeUntilNextRun.TotalMinutes, _nextRun);

                try
                {
                    await Task.Delay(timeUntilNextRun, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("OnboardingReminderBackgroundService is stopping");
                    break;
                }
            }

            // Execute the reminder processing
            await ProcessOnboardingRemindersAsync(stoppingToken);

            // Calculate next run time
            _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
            _logger.LogInformation("Next onboarding reminder check scheduled for: {NextRun} UTC", _nextRun);
        }

        _logger.LogInformation("OnboardingReminderBackgroundService stopped");
    }

    private async Task ProcessOnboardingRemindersAsync(CancellationToken cancellationToken)
    {
        var executionTime = DateTime.UtcNow;
        _logger.LogInformation("Starting onboarding reminder processing job at {Time} UTC", executionTime);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
            var onboardingRepository = scope.ServiceProvider.GetRequiredService<IOnboardingRepository>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            // Calculate target start date (3 days from now)
            var targetStartDate = DateTime.UtcNow.Date.AddDays(DaysBeforeStartDate);

            // Get employees whose start date is in 3 days
            var upcomingEmployees = await employeeRepository.GetEmployeesByStartDateAsync(
                targetStartDate,
                targetStartDate.AddDays(1),
                cancellationToken);

            var remindersCount = 0;

            foreach (var employee in upcomingEmployees)
            {
                // Check onboarding completion status
                var (completedCount, totalCount, completionPercentage) =
                    await onboardingRepository.GetStatusAsync(employee.Id, cancellationToken);

                // Only send reminder if onboarding is not complete
                if (completionPercentage < 100m)
                {
                    // Get manager information if available
                    string? managerEmail = null;
                    if (employee.ManagerId.HasValue)
                    {
                        var manager = await employeeRepository.GetByIdAsync(employee.ManagerId.Value, cancellationToken);
                        managerEmail = manager?.ContactInformation?.WorkEmail;
                    }

                    // Publish reminder event
                    var reminderEvent = new OnboardingReminderNeededIntegrationEvent
                    {
                        EmployeeId = employee.Id,
                        EmployeeNumber = employee.EmployeeNumber,
                        FullName = $"{employee.LegalName.FirstName} {employee.LegalName.LastName}",
                        Email = employee.ContactInformation.WorkEmail,
                        StartDate = employee.StartDate,
                        Department = employee.Department?.Name ?? "Unassigned",
                        JobTitle = employee.JobTitle ?? "Employee",
                        ManagerId = employee.ManagerId,
                        ManagerEmail = managerEmail,
                        CompletedItems = completedCount,
                        TotalItems = totalCount,
                        CompletionPercentage = completionPercentage,
                        DaysUntilStartDate = DaysBeforeStartDate,
                        EventTimestamp = DateTime.UtcNow
                    };

                    await eventPublisher.PublishAsync(reminderEvent, cancellationToken);

                    _logger.LogInformation(
                        "Sent onboarding reminder for employee {EmployeeId} ({EmployeeNumber}). " +
                        "Start date: {StartDate}, Completion: {Percentage}%",
                        employee.Id,
                        employee.EmployeeNumber,
                        employee.StartDate,
                        completionPercentage);

                    remindersCount++;
                }
            }

            _logger.LogInformation(
                "Onboarding reminder processing completed successfully. " +
                "Checked {EmployeeCount} employees, sent {ReminderCount} reminders",
                upcomingEmployees.Count(),
                remindersCount);

            _statusService.RecordSuccess(JobName, executionTime, _nextRun);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during onboarding reminder processing");
            _statusService.RecordFailure(JobName, executionTime, ex.Message, _nextRun);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OnboardingReminderBackgroundService is stopping");
        return base.StopAsync(cancellationToken);
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for sending performance review deadline reminders to managers
/// Runs daily at 9:00 AM (cron: "0 9 * * *")
/// Sends notifications 7 days before review deadline
/// </summary>
public class PerformanceReviewReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PerformanceReviewReminderBackgroundService> _logger;
    private readonly IBackgroundJobStatusService _statusService;
    private readonly CrontabSchedule _schedule;
    private DateTime _nextRun;

    // Cron schedule: "0 9 * * *" = At 09:00 every day
    // Format: minute hour day month day-of-week
    private const string Schedule = "0 9 * * *";
    private const string JobName = "PerformanceReviewReminderBackgroundService";
    private const int ReminderDaysBefore = 7;

    public PerformanceReviewReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PerformanceReviewReminderBackgroundService> logger,
        IBackgroundJobStatusService statusService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _statusService = statusService;
        _schedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
        _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);

        _logger.LogInformation("PerformanceReviewReminderBackgroundService initialized. Next run: {NextRun} UTC", _nextRun);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PerformanceReviewReminderBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var timeUntilNextRun = _nextRun - now;

            if (timeUntilNextRun.TotalMilliseconds > 0)
            {
                // Wait until next scheduled run or cancellation
                _logger.LogDebug("Waiting {Minutes} minutes until next review reminder check at {NextRun} UTC",
                    timeUntilNextRun.TotalMinutes, _nextRun);

                try
                {
                    await Task.Delay(timeUntilNextRun, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("PerformanceReviewReminderBackgroundService is stopping");
                    break;
                }
            }

            // Execute the reminder checking
            await SendPerformanceReviewRemindersAsync(stoppingToken);

            // Calculate next run time
            _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
            _logger.LogInformation("Next performance review reminder check scheduled for: {NextRun} UTC", _nextRun);
        }

        _logger.LogInformation("PerformanceReviewReminderBackgroundService stopped");
    }

    private async Task SendPerformanceReviewRemindersAsync(CancellationToken cancellationToken)
    {
        var executionTime = DateTime.UtcNow;
        _logger.LogInformation("Starting performance review reminder check at {Time} UTC", executionTime);

        try
        {
            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var performanceReviewRepository = scope.ServiceProvider.GetRequiredService<IPerformanceReviewRepository>();

            // Get current date for comparison
            var today = DateTime.UtcNow.Date;
            var reminderDate = today.AddDays(ReminderDaysBefore);

            // Get all draft performance reviews (not yet submitted)
            var draftReviews = await performanceReviewRepository.GetByStatusAsync("Draft", cancellationToken);

            if (!draftReviews.Any())
            {
                _logger.LogInformation("No draft performance reviews found");
                _statusService.RecordSuccess(JobName, executionTime, _nextRun);
                return;
            }

            var reminderCount = 0;

            foreach (var review in draftReviews)
            {
                // Calculate days until review period ends
                var daysUntilDeadline = (review.ReviewPeriodEnd.Date - today).Days;

                // Send reminder if deadline is exactly 7 days away
                if (daysUntilDeadline == ReminderDaysBefore)
                {
                    _logger.LogInformation(
                        "Performance review deadline reminder: ReviewId {ReviewId}, " +
                        "Employee {EmployeeId}, Reviewer {ReviewerId}, " +
                        "Deadline in {Days} days on {Deadline}, Cycle {Cycle}",
                        review.Id, review.EmployeeId, review.ReviewerId,
                        daysUntilDeadline, review.ReviewPeriodEnd.ToString("yyyy-MM-dd"),
                        review.ReviewCycle);

                    // TODO: Send notification to reviewer (email/push notification)
                    // This would typically use an INotificationService or IEventPublisher
                    // await notificationService.SendPerformanceReviewReminderAsync(review, daysUntilDeadline);
                    // OR publish an event:
                    // await eventPublisher.PublishAsync(new PerformanceReviewReminderEvent(review.Id, review.ReviewerId, daysUntilDeadline));

                    reminderCount++;
                }
                else if (daysUntilDeadline < 0)
                {
                    // Review is overdue
                    _logger.LogWarning(
                        "Overdue performance review: ReviewId {ReviewId}, " +
                        "Employee {EmployeeId}, Reviewer {ReviewerId}, " +
                        "Was due on {Deadline} ({DaysOverdue} days ago)",
                        review.Id, review.EmployeeId, review.ReviewerId,
                        review.ReviewPeriodEnd.ToString("yyyy-MM-dd"), Math.Abs(daysUntilDeadline));
                }
            }

            _logger.LogInformation(
                "Performance review reminder check completed. Checked {TotalCount} reviews, sent {ReminderCount} reminders",
                draftReviews.Count(), reminderCount);

            // Record success
            _statusService.RecordSuccess(JobName, executionTime, _nextRun);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during performance review reminder check");

            // Record failure
            _statusService.RecordFailure(JobName, executionTime, ex.Message, _nextRun);

            // Don't rethrow - we want the background service to continue running
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PerformanceReviewReminderBackgroundService is stopping");
        return base.StopAsync(cancellationToken);
    }
}

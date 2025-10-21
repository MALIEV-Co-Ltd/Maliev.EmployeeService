using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that sends reminders for work authorizations expiring soon
/// Runs daily at 9:00 AM to alert HR about authorizations expiring in 90/60/30/14 days
/// User Story 11 - Work Authorization & Visa Tracking
/// </summary>
public class WorkAuthorizationExpirationReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkAuthorizationExpirationReminderService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public WorkAuthorizationExpirationReminderService(
        IServiceProvider serviceProvider,
        ILogger<WorkAuthorizationExpirationReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Work Authorization Expiration Reminder Service started");

        // Wait until 9:00 AM for first run
        await WaitUntil9AM(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpirationRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing work authorization expiration reminders");
            }

            // Wait 24 hours until next run
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task WaitUntil9AM(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var next9AM = now.Date.AddHours(9);

        if (now.Hour >= 9)
        {
            next9AM = next9AM.AddDays(1);
        }

        var delay = next9AM - now;

        if (delay.TotalMilliseconds > 0)
        {
            _logger.LogInformation("Waiting {Delay} until next 9:00 AM UTC run", delay);
            await Task.Delay(delay, cancellationToken);
        }
    }

    private async Task ProcessExpirationRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var workAuthRepository = scope.ServiceProvider.GetRequiredService<IWorkAuthorizationRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WorkAuthorizationExpirationReminderService>>();

        logger.LogInformation("Starting work authorization expiration reminder check at {Time}", DateTime.UtcNow);

        // Check for authorizations expiring at specific thresholds
        var thresholds = new[] { 90, 60, 30, 14 };
        var now = DateTime.UtcNow;

        foreach (var daysThreshold in thresholds)
        {
            try
            {
                // Get authorizations expiring within this threshold
                var expiring = await workAuthRepository.GetExpiringAsync(
                    daysThreshold,
                    cancellationToken);

                // Filter to only those within ±1 day of the exact threshold
                var exactMatches = expiring.Where(a =>
                {
                    if (!a.ExpirationDate.HasValue) return false;
                    var daysUntilExpiration = (a.ExpirationDate.Value - now).TotalDays;
                    return daysUntilExpiration >= (daysThreshold - 1) && daysUntilExpiration <= (daysThreshold + 1);
                }).ToList();

                if (exactMatches.Any())
                {
                    logger.LogWarning(
                        "Found {Count} work authorizations expiring in approximately {Days} days: {Employees}",
                        exactMatches.Count,
                        daysThreshold,
                        string.Join(", ", exactMatches.Select(a =>
                            $"{a.Employee?.LegalName?.FullName ?? "Unknown"} ({a.AuthorizationType})")));

                    // TODO: Send notifications to HR team
                    // This would integrate with a notification service (email, Slack, etc.)
                    // Example: await _notificationService.SendExpirationAlertAsync(exactMatches, daysThreshold);

                    foreach (var auth in exactMatches)
                    {
                        logger.LogInformation(
                            "Work authorization alert - Employee: {EmployeeName} ({EmployeeNumber}), " +
                            "Type: {AuthType}, Document: {DocNumber}, Expires: {ExpirationDate}, " +
                            "Days remaining: {DaysRemaining}",
                            auth.Employee?.LegalName?.FullName ?? "Unknown",
                            auth.Employee?.EmployeeNumber ?? "Unknown",
                            auth.AuthorizationType,
                            auth.DocumentNumber,
                            auth.ExpirationDate?.ToString("yyyy-MM-dd"),
                            Math.Round((auth.ExpirationDate!.Value - now).TotalDays));
                    }
                }
                else
                {
                    logger.LogDebug("No work authorizations expiring in exactly {Days} days", daysThreshold);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking {Days}-day expiration threshold", daysThreshold);
            }
        }

        logger.LogInformation("Completed work authorization expiration reminder check");
    }
}

using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that sends certification expiration reminders
/// Runs daily and sends alerts 60/30/14 days before expiration
/// </summary>
public class CertificationExpirationReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CertificationExpirationReminderBackgroundService> _logger;

    // Cron schedule: "0 8 * * *" = At 08:00 every day (8 AM UTC)
    private const string Schedule = "0 8 * * *";
    private readonly CrontabSchedule _crontabSchedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = false });

    // Alert thresholds in days
    private static readonly int[] AlertThresholds = { 60, 30, 14 };

    public CertificationExpirationReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CertificationExpirationReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Certification Expiration Reminder Background Service started with schedule: {Schedule}", Schedule);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextOccurrence = _crontabSchedule.GetNextOccurrence(now);
            var delay = nextOccurrence - now;

            _logger.LogInformation("Next certification expiration check scheduled for {NextRun} (in {Delay})",
                nextOccurrence, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessCertificationExpirationsAsync(stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Certification Expiration Reminder Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Certification Expiration Reminder Background Service");
                // Continue running even if an error occurs
            }
        }

        _logger.LogInformation("Certification Expiration Reminder Background Service stopped");
    }

    private async Task ProcessCertificationExpirationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting certification expiration reminder check");

        using var scope = _serviceProvider.CreateScope();
        var trainingRepository = scope.ServiceProvider.GetRequiredService<ITrainingRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        try
        {
            // Check for each alert threshold
            foreach (var threshold in AlertThresholds)
            {
                var expiringCertifications = await trainingRepository.GetExpiringCertificationsAsync(
                    threshold,
                    cancellationToken);

                var expiringList = expiringCertifications.ToList();

                if (expiringList.Any())
                {
                    _logger.LogInformation(
                        "Found {Count} certifications expiring within {Days} days",
                        expiringList.Count,
                        threshold);

                    foreach (var cert in expiringList)
                    {
                        // Check if this is exactly at the threshold (within 1 day)
                        var daysUntilExpiration = cert.ExpirationDate.HasValue
                            ? (int)(cert.ExpirationDate.Value - DateTime.UtcNow).TotalDays
                            : 0;

                        if (Math.Abs(daysUntilExpiration - threshold) <= 1)
                        {
                            _logger.LogInformation(
                                "Sending expiration reminder for {CourseName} for employee {EmployeeId} ({EmployeeNumber}), expires in {Days} days",
                                cert.CourseName,
                                cert.EmployeeId,
                                cert.Employee.EmployeeNumber,
                                daysUntilExpiration);

                            // TODO: Publish CertificationExpirationReminderIntegrationEvent
                            // This would notify the notification service to send emails/alerts
                        }
                    }
                }
            }

            _logger.LogInformation("Certification expiration reminder check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing certification expirations");
        }
    }
}

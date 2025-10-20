using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that sends reminders for expiring documents
/// Runs daily at 8:00 AM ICT to check for documents expiring in 90, 60, 30, and 14 days
/// </summary>
public class DocumentExpirationReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentExpirationReminderBackgroundService> _logger;
    private readonly CrontabSchedule _schedule;
    private DateTime _nextRun;

    // Run daily at 8:00 AM ICT
    private const string Schedule = "0 8 * * *";

    public DocumentExpirationReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DocumentExpirationReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _schedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
        _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Expiration Reminder Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var timeUntilNextRun = _nextRun - now;

            if (timeUntilNextRun <= TimeSpan.Zero)
            {
                // Time to run the job
                await ProcessDocumentExpirationRemindersAsync(stoppingToken);

                // Calculate next run time
                _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
                _logger.LogInformation("Next document expiration reminder check scheduled for {NextRun} UTC", _nextRun);
            }
            else
            {
                // Wait until next scheduled run
                _logger.LogDebug("Next document expiration reminder check in {TimeSpan}", timeUntilNextRun);
                await Task.Delay(timeUntilNextRun > TimeSpan.FromMinutes(1)
                    ? TimeSpan.FromMinutes(1)
                    : timeUntilNextRun, stoppingToken);
            }
        }

        _logger.LogInformation("Document Expiration Reminder Background Service stopped");
    }

    private async Task ProcessDocumentExpirationRemindersAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing document expiration reminders");

            using var scope = _serviceProvider.CreateScope();
            var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

            // Check documents expiring at different thresholds
            var thresholds = new[] { 90, 60, 30, 14 };
            var totalReminders = 0;

            foreach (var days in thresholds)
            {
                var expiringDocuments = await documentRepository.GetExpiringDocumentsAsync(days, cancellationToken);
                var documentsList = expiringDocuments.ToList();

                if (documentsList.Any())
                {
                    _logger.LogInformation("Found {Count} documents expiring within {Days} days",
                        documentsList.Count, days);

                    foreach (var document in documentsList)
                    {
                        // TODO: Send notification to employee and HR
                        // For now, just log
                        _logger.LogInformation(
                            "Document {DocumentId} ({DocumentType}) for employee {EmployeeId} expires on {ExpirationDate} ({DaysRemaining} days remaining)",
                            document.Id,
                            document.DocumentType,
                            document.EmployeeId,
                            document.ExpirationDate,
                            document.ExpirationDate.HasValue
                                ? (int)(document.ExpirationDate.Value - DateTime.UtcNow).TotalDays
                                : 0);

                        totalReminders++;
                    }
                }
            }

            // Check already expired documents
            var expiredDocuments = await documentRepository.GetExpiredDocumentsAsync(cancellationToken);
            var expiredList = expiredDocuments.ToList();

            if (expiredList.Any())
            {
                _logger.LogWarning("Found {Count} expired documents requiring urgent attention",
                    expiredList.Count);

                foreach (var document in expiredList)
                {
                    // TODO: Send urgent notification to HR
                    _logger.LogWarning(
                        "EXPIRED: Document {DocumentId} ({DocumentType}) for employee {EmployeeId} expired on {ExpirationDate}",
                        document.Id,
                        document.DocumentType,
                        document.EmployeeId,
                        document.ExpirationDate);

                    totalReminders++;
                }
            }

            _logger.LogInformation("Document expiration reminder check completed. Processed {TotalReminders} reminders",
                totalReminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document expiration reminders");
        }
    }
}

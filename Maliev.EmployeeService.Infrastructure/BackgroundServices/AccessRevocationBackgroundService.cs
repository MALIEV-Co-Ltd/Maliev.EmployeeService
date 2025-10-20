using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.IntegrationEvents;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for automatic access revocation on termination date
/// Runs daily at midnight UTC (cron: "0 0 * * *")
/// Checks for employees whose termination date is today and triggers access revocation
/// </summary>
public class AccessRevocationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AccessRevocationBackgroundService> _logger;
    private readonly IBackgroundJobStatusService _statusService;
    private readonly CrontabSchedule _schedule;
    private DateTime _nextRun;

    // Cron schedule: "0 0 * * *" = At 00:00 every day (midnight UTC)
    // Format: minute hour day month day-of-week
    private const string Schedule = "0 0 * * *";
    private const string JobName = "AccessRevocationBackgroundService";

    public AccessRevocationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AccessRevocationBackgroundService> logger,
        IBackgroundJobStatusService statusService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _statusService = statusService;
        _schedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = false });
        _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);

        _logger.LogInformation("AccessRevocationBackgroundService initialized. Next run: {NextRun} UTC", _nextRun);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AccessRevocationBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var timeUntilNextRun = _nextRun - now;

            if (timeUntilNextRun.TotalMilliseconds > 0)
            {
                _logger.LogDebug("Waiting {Minutes} minutes until next access revocation check at {NextRun} UTC",
                    timeUntilNextRun.TotalMinutes, _nextRun);

                try
                {
                    await Task.Delay(timeUntilNextRun, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("AccessRevocationBackgroundService is stopping");
                    break;
                }
            }

            // Execute the access revocation processing
            await ProcessAccessRevocationAsync(stoppingToken);

            // Calculate next run time
            _nextRun = _schedule.GetNextOccurrence(DateTime.UtcNow);
            _logger.LogInformation("Next access revocation check scheduled for: {NextRun} UTC", _nextRun);
        }

        _logger.LogInformation("AccessRevocationBackgroundService stopped");
    }

    private async Task ProcessAccessRevocationAsync(CancellationToken cancellationToken)
    {
        var executionTime = DateTime.UtcNow;
        _logger.LogInformation("Starting access revocation processing job at {Time} UTC", executionTime);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            // Get employees whose termination date is today or earlier (catch-up for missed runs)
            var today = DateTime.UtcNow.Date;
            var terminatedEmployees = await employeeRepository.GetEmployeesByTerminationDateAsync(
                today,
                cancellationToken);

            var revocationsCount = 0;

            foreach (var employee in terminatedEmployees)
            {
                // Only process if termination date is today or past
                if (employee.TerminationDate.HasValue && employee.TerminationDate.Value.Date <= today)
                {
                    // Publish access revocation event
                    var revocationEvent = new AccessRevocationRequiredIntegrationEvent
                    {
                        EmployeeId = employee.Id,
                        EmployeeNumber = employee.EmployeeNumber,
                        FullName = $"{employee.LegalName.FirstName} {employee.LegalName.LastName}",
                        Email = employee.ContactInformation.WorkEmail,
                        TerminationDate = employee.TerminationDate.Value,
                        Department = employee.Department?.Name ?? "Unknown",
                        JobTitle = employee.JobTitle ?? "Employee",
                        ManagerId = employee.ManagerId,
                        TerminationReason = null, // Could be retrieved from exit interview if needed
                        EventTimestamp = DateTime.UtcNow
                    };

                    await eventPublisher.PublishAsync(revocationEvent, cancellationToken);

                    _logger.LogInformation(
                        "Triggered access revocation for employee {EmployeeId} ({EmployeeNumber}). " +
                        "Termination date: {TerminationDate}",
                        employee.Id,
                        employee.EmployeeNumber,
                        employee.TerminationDate.Value);

                    revocationsCount++;
                }
            }

            _logger.LogInformation(
                "Access revocation processing completed successfully. " +
                "Checked {EmployeeCount} terminated employees, triggered {RevocationCount} revocations",
                terminatedEmployees.Count(),
                revocationsCount);

            _statusService.RecordSuccess(JobName, executionTime, _nextRun);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during access revocation processing");
            _statusService.RecordFailure(JobName, executionTime, ex.Message, _nextRun);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AccessRevocationBackgroundService is stopping");
        return base.StopAsync(cancellationToken);
    }
}

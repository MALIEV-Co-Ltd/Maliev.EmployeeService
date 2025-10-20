using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that escalates overdue mandatory training to manager and HR
/// Runs daily and checks for overdue mandatory training
/// </summary>
public class OverdueTrainingEscalationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverdueTrainingEscalationBackgroundService> _logger;

    // Cron schedule: "0 9 * * *" = At 09:00 every day (9 AM UTC)
    private const string Schedule = "0 9 * * *";
    private readonly CrontabSchedule _crontabSchedule = CrontabSchedule.Parse(Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = false });

    public OverdueTrainingEscalationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OverdueTrainingEscalationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Overdue Training Escalation Background Service started with schedule: {Schedule}", Schedule);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextOccurrence = _crontabSchedule.GetNextOccurrence(now);
            var delay = nextOccurrence - now;

            _logger.LogInformation("Next overdue training escalation check scheduled for {NextRun} (in {Delay})",
                nextOccurrence, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessOverdueTrainingAsync(stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Overdue Training Escalation Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Overdue Training Escalation Background Service");
                // Continue running even if an error occurs
            }
        }

        _logger.LogInformation("Overdue Training Escalation Background Service stopped");
    }

    private async Task ProcessOverdueTrainingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting overdue training escalation check");

        using var scope = _serviceProvider.CreateScope();
        var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        var trainingRepository = scope.ServiceProvider.GetRequiredService<ITrainingRepository>();
        var mandatoryTrainingRepository = scope.ServiceProvider.GetRequiredService<IMandatoryTrainingRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        try
        {
            // Get all active employees
            var activeEmployees = await employeeRepository.GetByStatusAsync(EmploymentStatus.Active, cancellationToken);

            var escalationCount = 0;

            foreach (var employee in activeEmployees)
            {
                // Get mandatory training requirements for this employee
                var requirements = await mandatoryTrainingRepository.GetActiveRequirementsForEmployeeAsync(
                    employee.EmploymentType,
                    employee.JobTitle,
                    cancellationToken);

                if (!requirements.Any())
                {
                    continue;
                }

                // Get employee's completed training
                var completedTraining = await trainingRepository.GetByTypeAsync(
                    employee.Id,
                    TrainingType.Mandatory,
                    cancellationToken);

                var completedCourseNames = completedTraining.Select(tr => tr.CourseName).ToList();

                // Check for missing mandatory training
                var missingTraining = new List<string>();

                foreach (var requirement in requirements)
                {
                    var requiredCourses = requirement.GetRequiredCoursesList();
                    var missing = requiredCourses.Where(c => !completedCourseNames.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();

                    if (missing.Any())
                    {
                        // Check if deadline has passed
                        var deadlineDate = employee.StartDate.AddDays(requirement.DeadlineDaysFromStart);
                        if (DateTime.UtcNow.Date > deadlineDate)
                        {
                            missingTraining.AddRange(missing);
                        }
                    }
                }

                if (missingTraining.Any())
                {
                    escalationCount++;

                    _logger.LogWarning(
                        "Employee {EmployeeId} ({EmployeeNumber}) has {Count} overdue mandatory training courses: {Courses}",
                        employee.Id,
                        employee.EmployeeNumber,
                        missingTraining.Count,
                        string.Join(", ", missingTraining));

                    // TODO: Publish OverdueTrainingEscalationIntegrationEvent
                    // This would notify manager and HR about overdue training
                }
            }

            _logger.LogInformation(
                "Overdue training escalation check completed. Escalated {Count} employees",
                escalationCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing overdue training escalation");
        }
    }
}

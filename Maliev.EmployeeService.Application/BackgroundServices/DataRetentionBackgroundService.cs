using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.BackgroundServices;

public class DataRetentionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataRetentionBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1);

    public DataRetentionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DataRetentionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DataRetentionBackgroundService starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AnonymizeExpiredRecordsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while anonymizing records");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("DataRetentionBackgroundService stopping");
    }

    private async Task AnonymizeExpiredRecordsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var cutoffDate = DateTime.UtcNow.AddYears(-7);
        var employeesToAnonymize = await employeeRepository.GetEmployeesByTerminationDateAsync(cutoffDate, cancellationToken);

        if (employeesToAnonymize == null || !employeesToAnonymize.Any())
        {
            return;
        }

        _logger.LogInformation("Found {Count} employees to anonymize", employeesToAnonymize.Count());

        foreach (var employee in employeesToAnonymize)
        {
            if (employee.AnonymizedAt.HasValue) continue;

            employee.PreferredName = "Anonymized";
            employee.LegalName.FirstName = "Anonymized";
            employee.LegalName.LastName = "Anonymized";
            employee.LegalName.MiddleName = null;
            employee.NationalId = "Anonymized";
            employee.ContactInformation.PersonalEmail = "anonymized@maliev.com";
            employee.ContactInformation.WorkEmail = $"anonymized_{employee.Id}@maliev.com";
            employee.ContactInformation.MobilePhone = "000-000-0000";
            employee.AnonymizedAt = DateTime.UtcNow;

            employeeRepository.Update(employee);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully anonymized records");
    }
}

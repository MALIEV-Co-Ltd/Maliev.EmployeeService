using Maliev.EmployeeService.Domain.Sagas;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that monitors for stalled EmployeeTerminationSaga instances
/// and attempts to recover or re-trigger missing steps.
/// </summary>
public class SagaRecoveryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaRecoveryService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _staleThreshold = TimeSpan.FromHours(2);

    public SagaRecoveryService(
        IServiceProvider serviceProvider,
        ILogger<SagaRecoveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SagaRecoveryService starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecoverStalledSagasAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during saga recovery");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("SagaRecoveryService stopping");
    }

    private async Task RecoverStalledSagasAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var staleDate = DateTime.UtcNow.Subtract(_staleThreshold);

        // Find sagas stuck in Processing state for more than the threshold
        // Using EmployeeTerminationSagaState which is the MassTransit state machine instance
        var stalledSagas = await context.Set<EmployeeTerminationSagaState>()
            .Where(s => s.CurrentState == "Processing" && (s.ModifiedDate ?? s.CreatedDate) < staleDate)
            .ToListAsync(cancellationToken);

        if (!stalledSagas.Any()) return;

        _logger.LogInformation("Found {Count} stalled termination sagas. Attempting recovery.", stalledSagas.Count);

        foreach (var saga in stalledSagas)
        {
            _logger.LogWarning("Recovering saga for employee {EmployeeId} (CorrelationId: {CorrelationId})",
                saga.EmployeeId, saga.CorrelationId);

            // Logic to re-trigger the next step based on flags
            if (!saga.LeaveBalanceClosed)
            {
                await bus.Publish(new CloseLeaveBalanceCommand
                {
                    EmployeeId = saga.EmployeeId,
                    TerminationDate = saga.TerminationDate
                }, cancellationToken);
            }
            else if (!saga.CompensationArchived)
            {
                await bus.Publish(new ArchiveCompensationCommand { EmployeeId = saga.EmployeeId }, cancellationToken);
            }
            else if (!saga.AccessRevoked)
            {
                await bus.Publish(new RevokeAccessCommand { EmployeeId = saga.EmployeeId }, cancellationToken);
            }

            // Update timestamp to avoid immediate re-processing
            saga.ModifiedDate = DateTime.UtcNow;
            context.Update(saga);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

// Internal commands repeated for visibility within background service context
public class CloseLeaveBalanceCommand { public Guid EmployeeId { get; set; } public DateTime TerminationDate { get; set; } }
public class ArchiveCompensationCommand { public Guid EmployeeId { get; set; } }
public class RevokeAccessCommand { public Guid EmployeeId { get; set; } }

using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to mark an offboarding checklist item as complete
/// </summary>
public class CompleteOffboardingItemCommand
{
    public Guid ItemId { get; set; }
    public Guid CompletedByEmployeeId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Handler for completing offboarding checklist items
/// </summary>
public class CompleteOffboardingItemCommandHandler
{
    private readonly IOffboardingRepository _offboardingRepository;
    private readonly ILogger<CompleteOffboardingItemCommandHandler> _logger;

    public CompleteOffboardingItemCommandHandler(
        IOffboardingRepository offboardingRepository,
        ILogger<CompleteOffboardingItemCommandHandler> logger)
    {
        _offboardingRepository = offboardingRepository;
        _logger = logger;
    }

    public async Task HandleAsync(CompleteOffboardingItemCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Completing offboarding item {ItemId} by employee {EmployeeId}",
            command.ItemId,
            command.CompletedByEmployeeId);

        // Verify item exists
        var item = await _offboardingRepository.GetItemByIdAsync(command.ItemId, cancellationToken);
        if (item == null)
        {
            throw new InvalidOperationException($"Offboarding checklist item {command.ItemId} not found");
        }

        if (item.CompletionStatus)
        {
            _logger.LogWarning("Offboarding item {ItemId} is already completed", command.ItemId);
            return;
        }

        // Complete the item
        await _offboardingRepository.CompleteItemAsync(
            command.ItemId,
            command.CompletedByEmployeeId,
            command.Notes,
            cancellationToken);

        _logger.LogInformation(
            "Completed offboarding item {ItemId} for employee {EmployeeId}",
            command.ItemId,
            item.EmployeeId);

        // Check if all critical items (blocking paycheck) are complete
        var canFinalize = await _offboardingRepository.CanFinalizeAsync(item.EmployeeId, cancellationToken);
        if (canFinalize)
        {
            _logger.LogInformation(
                "Employee {EmployeeId} offboarding can be finalized - all paycheck-blocking items complete",
                item.EmployeeId);
        }
        else
        {
            var blockingItems = await _offboardingRepository.GetPaycheckBlockingItemsAsync(item.EmployeeId, cancellationToken);
            _logger.LogInformation(
                "Employee {EmployeeId} has {Count} remaining items blocking final paycheck",
                item.EmployeeId,
                blockingItems.Count());
        }
    }
}

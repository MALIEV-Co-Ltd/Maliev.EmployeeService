using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to mark an onboarding checklist item as complete
/// </summary>
public class CompleteOnboardingItemCommand
{
    public Guid ItemId { get; set; }
    public Guid CompletedByEmployeeId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Handler for completing onboarding checklist items
/// </summary>
public class CompleteOnboardingItemCommandHandler
{
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly ILogger<CompleteOnboardingItemCommandHandler> _logger;

    public CompleteOnboardingItemCommandHandler(
        IOnboardingRepository onboardingRepository,
        ILogger<CompleteOnboardingItemCommandHandler> logger)
    {
        _onboardingRepository = onboardingRepository;
        _logger = logger;
    }

    public async Task HandleAsync(CompleteOnboardingItemCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Completing onboarding item {ItemId} by employee {EmployeeId}",
            command.ItemId,
            command.CompletedByEmployeeId);

        // Verify item exists
        var item = await _onboardingRepository.GetItemByIdAsync(command.ItemId, cancellationToken);
        if (item == null)
        {
            throw new InvalidOperationException($"Onboarding checklist item {command.ItemId} not found");
        }

        if (item.CompletionStatus)
        {
            _logger.LogWarning("Onboarding item {ItemId} is already completed", command.ItemId);
            return;
        }

        // Complete the item
        await _onboardingRepository.CompleteItemAsync(
            command.ItemId,
            command.CompletedByEmployeeId,
            command.Notes,
            cancellationToken);

        _logger.LogInformation(
            "Completed onboarding item {ItemId} for employee {EmployeeId}",
            command.ItemId,
            item.EmployeeId);

        // Check if all items are complete
        var status = await _onboardingRepository.GetStatusAsync(item.EmployeeId, cancellationToken);
        if (status.CompletionPercentage == 100)
        {
            _logger.LogInformation(
                "Employee {EmployeeId} has completed all onboarding items",
                item.EmployeeId);
        }
    }
}

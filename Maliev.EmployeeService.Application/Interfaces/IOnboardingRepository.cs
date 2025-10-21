using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for onboarding checklist management
/// </summary>
public interface IOnboardingRepository
{
    /// <summary>
    /// Creates onboarding checklist items for an employee
    /// </summary>
    Task CreateChecklistAsync(IEnumerable<OnboardingChecklist> checklistItems, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all onboarding checklist items for an employee with completion status
    /// </summary>
    Task<IEnumerable<OnboardingChecklist>> GetChecklistAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets onboarding status summary (completion percentage, pending items count)
    /// </summary>
    Task<(int CompletedCount, int TotalCount, decimal CompletionPercentage)> GetStatusAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a checklist item as complete
    /// </summary>
    Task CompleteItemAsync(Guid itemId, Guid completedByEmployeeId, string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific checklist item by ID
    /// </summary>
    Task<OnboardingChecklist?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all incomplete checklist items that are overdue
    /// </summary>
    Task<IEnumerable<OnboardingChecklist>> GetOverdueItemsAsync(CancellationToken cancellationToken = default);
}

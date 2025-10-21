using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for offboarding checklist management
/// </summary>
public interface IOffboardingRepository
{
    /// <summary>
    /// Creates offboarding checklist items for an employee
    /// </summary>
    Task CreateChecklistAsync(IEnumerable<OffboardingChecklist> checklistItems, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all offboarding checklist items for an employee with completion status
    /// </summary>
    Task<IEnumerable<OffboardingChecklist>> GetChecklistAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets offboarding status summary (completion percentage, pending items count)
    /// </summary>
    Task<(int CompletedCount, int TotalCount, decimal CompletionPercentage)> GetStatusAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a checklist item as complete
    /// </summary>
    Task CompleteItemAsync(Guid itemId, Guid completedByEmployeeId, string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific checklist item by ID
    /// </summary>
    Task<OffboardingChecklist?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all incomplete checklist items that are overdue
    /// </summary>
    Task<IEnumerable<OffboardingChecklist>> GetOverdueItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if offboarding can be finalized (all critical items completed)
    /// </summary>
    /// <remarks>
    /// Returns true only if all items marked as BlocksFinalPaycheck are completed
    /// </remarks>
    Task<bool> CanFinalizeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all items that block final paycheck release
    /// </summary>
    Task<IEnumerable<OffboardingChecklist>> GetPaycheckBlockingItemsAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

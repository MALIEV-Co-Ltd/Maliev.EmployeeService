using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Offboarding checklist item entity
/// Tracks individual tasks required for employee offboarding
/// </summary>
public class OffboardingChecklist : Entity
{
    /// <summary>
    /// Employee ID this checklist item belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Navigation property to Employee
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Description of the offboarding item
    /// </summary>
    public string ItemDescription { get; set; } = string.Empty;

    /// <summary>
    /// Party responsible for completing this item
    /// </summary>
    public ResponsibleParty ResponsibleParty { get; set; }

    /// <summary>
    /// Due date for completion
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Completion status of this item
    /// </summary>
    public bool CompletionStatus { get; set; }

    /// <summary>
    /// Date when the item was completed
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Employee ID of the person who completed this item
    /// </summary>
    public Guid? CompletedBy { get; set; }

    /// <summary>
    /// Navigation property to the employee who completed the item
    /// </summary>
    public Employee? CompletedByEmployee { get; set; }

    /// <summary>
    /// Notes or comments about the completion
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Display order for checklist items
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this item blocks final paycheck release
    /// </summary>
    public bool BlocksFinalPaycheck { get; set; }
}

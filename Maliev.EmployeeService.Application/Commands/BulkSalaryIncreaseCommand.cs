namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to apply bulk salary increase
/// User Story 12 - Bulk Operations
/// </summary>
public class BulkSalaryIncreaseCommand
{
    /// <summary>
    /// Department to apply increase (optional - if null, applies to all)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Percentage increase (e.g., 5 for 5%)
    /// </summary>
    public decimal PercentageIncrease { get; set; }

    /// <summary>
    /// Reason for the increase
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Effective date of the increase
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Preview mode - calculate changes without applying
    /// </summary>
    public bool PreviewOnly { get; set; } = true;

    /// <summary>
    /// User initiating the operation
    /// </summary>
    public required Guid InitiatedByUserId { get; set; }
}

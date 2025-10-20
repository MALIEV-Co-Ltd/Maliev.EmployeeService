using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Represents an employee objective or goal with success criteria and progress tracking
/// Implements FR-049
/// </summary>
public class Goal : Entity
{
    /// <summary>
    /// Employee who owns this goal
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Optional performance review this goal is associated with
    /// </summary>
    public Guid? PerformanceReviewId { get; set; }

    /// <summary>
    /// Goal description or title
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Measurable success criteria for goal completion
    /// </summary>
    public string? SuccessCriteria { get; set; }

    /// <summary>
    /// Target completion date
    /// </summary>
    public DateTime TargetDate { get; set; }

    /// <summary>
    /// Current status of the goal (NotStarted, InProgress, Completed, Cancelled)
    /// </summary>
    public GoalStatus CompletionStatus { get; set; } = GoalStatus.NotStarted;

    /// <summary>
    /// Progress updates and notes (stored as JSON or delimited text)
    /// </summary>
    public string? ProgressUpdates { get; set; }

    /// <summary>
    /// Actual completion date (when status changed to Completed)
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public PerformanceReview? PerformanceReview { get; set; }
}

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for goal data
/// </summary>
public class GoalDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid? PerformanceReviewId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SuccessCriteria { get; set; }
    public DateTime TargetDate { get; set; }
    public string CompletionStatus { get; set; } = string.Empty;
    public string? ProgressUpdates { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for performance review data
/// </summary>
public class PerformanceReviewDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string ReviewCycle { get; set; } = string.Empty;
    public DateTime ReviewPeriodStart { get; set; }
    public DateTime ReviewPeriodEnd { get; set; }
    public string? Rating { get; set; }
    public string? Feedback { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? AcknowledgedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SelfAssessment { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

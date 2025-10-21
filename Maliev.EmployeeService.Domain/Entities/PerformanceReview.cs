using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Represents a performance evaluation conducted by a manager for an employee
/// Supports quarterly, semi-annual, and annual review cycles
/// Implements FR-045, FR-046, FR-047
/// </summary>
public class PerformanceReview : Entity
{
    /// <summary>
    /// Employee being reviewed
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Manager conducting the review
    /// </summary>
    public Guid ReviewerId { get; set; }

    /// <summary>
    /// Review cycle frequency (Quarterly, SemiAnnual, Annual)
    /// </summary>
    public ReviewCycle ReviewCycle { get; set; }

    /// <summary>
    /// Start date of the performance review period
    /// </summary>
    public DateTime ReviewPeriodStart { get; set; }

    /// <summary>
    /// End date of the performance review period
    /// </summary>
    public DateTime ReviewPeriodEnd { get; set; }

    /// <summary>
    /// Performance rating (1-5 scale)
    /// </summary>
    public PerformanceRating? Rating { get; set; }

    /// <summary>
    /// Written feedback from reviewer
    /// </summary>
    public string? Feedback { get; set; }

    /// <summary>
    /// Date when the review was completed
    /// </summary>
    public DateTime? ReviewDate { get; set; }

    /// <summary>
    /// Date when employee acknowledged the review
    /// </summary>
    public DateTime? AcknowledgedDate { get; set; }

    /// <summary>
    /// Review status (Draft, Submitted, Acknowledged)
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Employee's self-assessment comments
    /// </summary>
    public string? SelfAssessment { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Employee Reviewer { get; set; } = null!;
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
}

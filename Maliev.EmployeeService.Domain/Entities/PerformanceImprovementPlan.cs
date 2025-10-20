using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Represents a Performance Improvement Plan (PIP) for employees not meeting performance expectations
/// Documents issues, improvement milestones, and tracks progress
/// Implements FR-048
/// </summary>
public class PerformanceImprovementPlan : Entity
{
    /// <summary>
    /// Employee on the performance improvement plan
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Manager overseeing the PIP
    /// </summary>
    public Guid ManagerId { get; set; }

    /// <summary>
    /// Documented performance issues requiring improvement
    /// </summary>
    public string IssuesDocumented { get; set; } = string.Empty;

    /// <summary>
    /// Improvement milestones with deadlines (stored as JSON or delimited text)
    /// </summary>
    public string Milestones { get; set; } = string.Empty;

    /// <summary>
    /// PIP start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// PIP end date (typically 30-90 days)
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Current status (Active, Successful, Unsuccessful, Cancelled)
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Progress updates and notes
    /// </summary>
    public string? ProgressNotes { get; set; }

    /// <summary>
    /// Final outcome and next steps
    /// </summary>
    public string? Outcome { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Employee Manager { get; set; } = null!;
}

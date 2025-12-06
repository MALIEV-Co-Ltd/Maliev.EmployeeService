namespace Maliev.EmployeeService.Application.DTOs;

///<summary>
/// DTO for span of control report
/// Identifies managers at or exceeding span of control limits
/// User Story 12 - Reporting & Analytics
/// </summary>
public class SpanOfControlReportDto
{
    ///<summary>
    /// Date the report was generated
    /// </summary>
    public DateTime GeneratedDate { get; set; }

    ///<summary>
    /// Total number of managers analyzed
    /// </summary>
    public int TotalManagers { get; set; }

    ///<summary>
    /// Managers exceeding span of control limits
    /// </summary>
    public List<ManagerSpanDto> ExceedingLimit { get; set; } = new();

    ///<summary>
    /// Managers at 80-100% of span of control capacity (warning threshold)
    /// </summary>
    public List<ManagerSpanDto> ApproachingLimit { get; set; } = new();

    ///<summary>
    /// Managers within normal range (&lt; 80% capacity)
    /// </summary>
    public List<ManagerSpanDto> WithinLimit { get; set; } = new();

    ///<summary>
    /// Summary statistics
    /// </summary>
    public SpanOfControlSummaryDto Summary { get; set; } = new();
}

///<summary>
/// Span of control details for a specific manager
/// </summary>
public class ManagerSpanDto
{
    ///<summary>
    /// The ID of the manager.
    /// </summary>
    public Guid ManagerId { get; set; }
    ///<summary>
    /// The name of the manager.
    /// </summary>
    public string ManagerName { get; set; } = string.Empty;
    ///<summary>
    /// The title of the manager.
    /// </summary>
    public string? ManagerTitle { get; set; }
    ///<summary>
    /// The ID of the department the manager belongs to.
    /// </summary>
    public Guid? DepartmentId { get; set; }
    ///<summary>
    /// The name of the department the manager belongs to.
    /// </summary>
    public string? DepartmentName { get; set; }

    ///<summary>
    /// Current number of direct reports
    /// </summary>
    public int CurrentSpan { get; set; }

    ///<summary>
    /// Maximum allowed span for this manager type
    /// </summary>
    public int MaxSpan { get; set; }

    ///<summary>
    /// Percentage of capacity used (CurrentSpan / MaxSpan * 100)
    /// </summary>
    public decimal CapacityPercentage { get; set; }

    ///<summary>
    /// Whether this manager manages other managers
    /// </summary>
    public bool IsManagerOfManagers { get; set; }

    ///<summary>
    /// Number of direct reports who are themselves managers
    /// </summary>
    public int ManagerReportsCount { get; set; }
}

///<summary>
/// Summary statistics for span of control report
/// </summary>
public class SpanOfControlSummaryDto
{
    ///<summary>
    /// Number of managers exceeding limits
    /// </summary>
    public int ManagersExceedingLimit { get; set; }

    ///<summary>
    /// Number of managers approaching limits (80-100%)
    /// </summary>
    public int ManagersApproachingLimit { get; set; }

    ///<summary>
    /// Number of managers within normal range
    /// </summary>
    public int ManagersWithinLimit { get; set; }

    ///<summary>
    /// Average span of control across all managers
    /// </summary>
    public decimal AverageSpan { get; set; }

    ///<summary>
    /// Median span of control
    /// </summary>
    public decimal MedianSpan { get; set; }

    ///<summary>
    /// Highest span of control found
    /// </summary>
    public int MaxSpanFound { get; set; }
}

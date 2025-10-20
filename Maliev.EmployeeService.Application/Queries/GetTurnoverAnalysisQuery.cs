namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to generate turnover analysis report
/// User Story 12 - Reporting & Analytics
/// </summary>
public class GetTurnoverAnalysisQuery
{
    /// <summary>
    /// Analysis period start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Analysis period end date
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Optional: Filter by specific department
    /// </summary>
    public Guid? DepartmentId { get; set; }
}

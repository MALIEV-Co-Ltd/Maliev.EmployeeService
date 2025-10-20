namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get compensation analysis
/// Restricted to HR Specialist role or higher
/// User Story 12 - Reporting & Analytics
/// </summary>
public class GetCompensationAnalysisQuery
{
    /// <summary>
    /// Date for which to calculate analysis (defaults to current date)
    /// </summary>
    public DateTime? AsOfDate { get; set; }

    /// <summary>
    /// Optional department filter
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Optional job title filter
    /// </summary>
    public string? JobTitle { get; set; }
}

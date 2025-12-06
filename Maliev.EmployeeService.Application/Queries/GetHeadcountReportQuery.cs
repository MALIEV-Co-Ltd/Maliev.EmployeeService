namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Query to generate headcount report with various aggregations
/// User Story 12 - Reporting & Analytics
/// </summary>
public class GetHeadcountReportQuery
{
    ///<summary>
    /// Optional: Filter by specific department
    /// </summary>
    public Guid? DepartmentId { get; set; }

    ///<summary>
    /// Date to generate report as of (default: today)
    /// </summary>
    public DateTime? AsOfDate { get; set; }

    ///<summary>
    /// Group by dimension (department, employmentType, tenure, location)
    /// </summary>
    public string? GroupBy { get; set; }
}

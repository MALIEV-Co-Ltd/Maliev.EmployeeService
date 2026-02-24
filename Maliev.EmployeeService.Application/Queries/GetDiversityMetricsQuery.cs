namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Query to get diversity metrics
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class GetDiversityMetricsQuery
{
    ///<summary>
    /// Date for which to calculate metrics (defaults to current date)
    /// </summary>
    public DateTime? AsOfDate { get; set; }

    ///<summary>
    /// Optional department filter
    /// </summary>
    public Guid? DepartmentId { get; set; }
}

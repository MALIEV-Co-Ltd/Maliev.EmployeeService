namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Query to get span of control report
/// Identifies managers at or exceeding span limits
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class GetSpanOfControlReportQuery
{
    ///<summary>
    /// Optional department filter
    /// </summary>
    public Guid? DepartmentId { get; set; }

    ///<summary>
    /// Include only managers exceeding/approaching limits (default: false, includes all managers)
    /// </summary>
    public bool OnlyAtRisk { get; set; } = false;
}

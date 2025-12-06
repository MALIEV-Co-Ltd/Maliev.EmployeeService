namespace Maliev.EmployeeService.Application.Queries;

///<summary>
/// Query to get leave utilization report
/// Analyzes leave accrual, usage, and carryover patterns
/// User Story 12 - Reporting & Analytics
/// </summary>
public class GetLeaveUtilizationReportQuery
{
    ///<summary>
    /// Year for which to generate the report (defaults to current year)
    /// </summary>
    public int? Year { get; set; }

    ///<summary>
    /// Optional department filter
    /// </summary>
    public Guid? DepartmentId { get; set; }
}

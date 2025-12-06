namespace Maliev.EmployeeService.Application.DTOs;

///<summary>
/// DTO for headcount report with aggregations
/// User Story 12 - Reporting & Analytics
/// </summary>
public class HeadcountReportDto
{
    ///<summary>
    /// Total active employee count
    /// </summary>
    public int TotalHeadcount { get; set; }

    ///<summary>
    /// Report generation date
    /// </summary>
    public DateTime AsOfDate { get; set; }

    ///<summary>
    /// Headcount breakdown by department
    /// </summary>
    public List<DepartmentHeadcountDto> ByDepartment { get; set; } = new();

    ///<summary>
    /// Headcount breakdown by employment type
    /// </summary>
    public Dictionary<string, int> ByEmploymentType { get; set; } = new();

    ///<summary>
    /// Headcount breakdown by tenure band
    /// </summary>
    public Dictionary<string, int> ByTenureBand { get; set; } = new();

    ///<summary>
    /// Headcount breakdown by location (if available)
    /// </summary>
    public Dictionary<string, int> ByLocation { get; set; } = new();
}

///<summary>
/// Department-specific headcount data
/// </summary>
public class DepartmentHeadcountDto
{
    ///<summary>
    /// The ID of the department.
    /// </summary>
    public Guid DepartmentId { get; set; }
    ///<summary>
    /// The name of the department.
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;
    ///<summary>
    /// The total headcount in the department.
    /// </summary>
    public int Headcount { get; set; }
    ///<summary>
    /// The number of managers in the department.
    /// </summary>
    public int ManagerCount { get; set; }
    ///<summary>
    /// The number of individual contributors in the department.
    /// </summary>
    public int IndividualContributorCount { get; set; }
}

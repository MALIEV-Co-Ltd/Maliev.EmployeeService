namespace Maliev.EmployeeService.Application.DTOs;

///<summary>
/// DTO for diversity metrics report
/// User Story 12 - Reporting &amp; Analytics
/// </summary>
public class DiversityMetricsDto
{
    ///<summary>
    /// Total headcount analyzed
    /// </summary>
    public int TotalHeadcount { get; set; }

    ///<summary>
    /// Date the metrics were calculated
    /// </summary>
    public DateTime AsOfDate { get; set; }

    ///<summary>
    /// Gender distribution
    /// </summary>
    public Dictionary<string, int> ByGender { get; set; } = new();

    ///<summary>
    /// Nationality distribution
    /// </summary>
    public Dictionary<string, int> ByNationality { get; set; } = new();

    ///<summary>
    /// Age band distribution
    /// </summary>
    public Dictionary<string, int> ByAgeBand { get; set; } = new();

    ///<summary>
    /// Diversity metrics by department
    /// </summary>
    public List<DepartmentDiversityDto> ByDepartment { get; set; } = new();

    ///<summary>
    /// Gender representation in leadership roles
    /// </summary>
    public Dictionary<string, int> LeadershipByGender { get; set; } = new();
}

///<summary>
/// Diversity metrics for a specific department
/// </summary>
public class DepartmentDiversityDto
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
    /// The headcount in the department.
    /// </summary>
    public int Headcount { get; set; }
    ///<summary>
    /// The gender distribution within the department.
    /// </summary>
    public Dictionary<string, int> GenderDistribution { get; set; } = new();
    ///<summary>
    /// The nationality distribution within the department.
    /// </summary>
    public Dictionary<string, int> NationalityDistribution { get; set; } = new();
}

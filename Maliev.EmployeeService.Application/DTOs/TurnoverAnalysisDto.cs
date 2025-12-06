namespace Maliev.EmployeeService.Application.DTOs;

///<summary>
/// DTO for turnover analysis report
/// User Story 12 - Reporting & Analytics
/// </summary>
public class TurnoverAnalysisDto
{
    ///<summary>
    /// Analysis period start date
    /// </summary>
    public DateTime StartDate { get; set; }

    ///<summary>
    /// Analysis period end date
    /// </summary>
    public DateTime EndDate { get; set; }

    ///<summary>
    /// Average headcount during the period
    /// </summary>
    public int AverageHeadcount { get; set; }

    ///<summary>
    /// Total terminations during period
    /// </summary>
    public int TotalTerminations { get; set; }

    ///<summary>
    /// Voluntary terminations (resignations)
    /// </summary>
    public int VoluntaryTerminations { get; set; }

    ///<summary>
    /// Involuntary terminations (layoffs, terminations)
    /// </summary>
    public int InvoluntaryTerminations { get; set; }

    ///<summary>
    /// Overall turnover rate (percentage)
    /// </summary>
    public decimal TurnoverRate { get; set; }

    ///<summary>
    /// Voluntary turnover rate (percentage)
    /// </summary>
    public decimal VoluntaryTurnoverRate { get; set; }

    ///<summary>
    /// Involuntary turnover rate (percentage)
    /// </summary>
    public decimal InvoluntaryTurnoverRate { get; set; }

    ///<summary>
    /// Turnover breakdown by department
    /// </summary>
    public List<DepartmentTurnoverDto> ByDepartment { get; set; } = new();

    ///<summary>
    /// Monthly turnover trend
    /// </summary>
    public List<MonthlyTurnoverDto> MonthlyTrend { get; set; } = new();
}

///<summary>
/// Department-specific turnover data
/// </summary>
public class DepartmentTurnoverDto
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
    /// The headcount of the department.
    /// </summary>
    public int Headcount { get; set; }
    ///<summary>
    /// The number of terminations in the department.
    /// </summary>
    public int Terminations { get; set; }
    ///<summary>
    /// The turnover rate for the department.
    /// </summary>
    public decimal TurnoverRate { get; set; }
}

///<summary>
/// Monthly turnover trend data
/// </summary>
public class MonthlyTurnoverDto
{
    ///<summary>
    /// The year of the monthly data.
    /// </summary>
    public int Year { get; set; }
    ///<summary>
    /// The month of the data.
    /// </summary>
    public int Month { get; set; }
    ///<summary>
    /// The name of the month.
    /// </summary>
    public string MonthName { get; set; } = string.Empty;
    ///<summary>
    /// The number of terminations in the month.
    /// </summary>
    public int Terminations { get; set; }
    ///<summary>
    /// The average headcount for the month.
    /// </summary>
    public int AverageHeadcount { get; set; }
    ///<summary>
    /// The turnover rate for the month.
    /// </summary>
    public decimal TurnoverRate { get; set; }
}

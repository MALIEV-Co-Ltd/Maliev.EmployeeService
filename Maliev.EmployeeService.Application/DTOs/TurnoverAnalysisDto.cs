namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for turnover analysis report
/// User Story 12 - Reporting & Analytics
/// </summary>
public class TurnoverAnalysisDto
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
    /// Average headcount during the period
    /// </summary>
    public int AverageHeadcount { get; set; }

    /// <summary>
    /// Total terminations during period
    /// </summary>
    public int TotalTerminations { get; set; }

    /// <summary>
    /// Voluntary terminations (resignations)
    /// </summary>
    public int VoluntaryTerminations { get; set; }

    /// <summary>
    /// Involuntary terminations (layoffs, terminations)
    /// </summary>
    public int InvoluntaryTerminations { get; set; }

    /// <summary>
    /// Overall turnover rate (percentage)
    /// </summary>
    public decimal TurnoverRate { get; set; }

    /// <summary>
    /// Voluntary turnover rate (percentage)
    /// </summary>
    public decimal VoluntaryTurnoverRate { get; set; }

    /// <summary>
    /// Involuntary turnover rate (percentage)
    /// </summary>
    public decimal InvoluntaryTurnoverRate { get; set; }

    /// <summary>
    /// Turnover breakdown by department
    /// </summary>
    public List<DepartmentTurnoverDto> ByDepartment { get; set; } = new();

    /// <summary>
    /// Monthly turnover trend
    /// </summary>
    public List<MonthlyTurnoverDto> MonthlyTrend { get; set; } = new();
}

/// <summary>
/// Department-specific turnover data
/// </summary>
public class DepartmentTurnoverDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int Headcount { get; set; }
    public int Terminations { get; set; }
    public decimal TurnoverRate { get; set; }
}

/// <summary>
/// Monthly turnover trend data
/// </summary>
public class MonthlyTurnoverDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int Terminations { get; set; }
    public int AverageHeadcount { get; set; }
    public decimal TurnoverRate { get; set; }
}

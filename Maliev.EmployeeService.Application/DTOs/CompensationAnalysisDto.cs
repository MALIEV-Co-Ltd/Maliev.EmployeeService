namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for compensation analysis report
/// Contains anonymized salary ranges and statistics
/// User Story 12 - Reporting & Analytics
/// </summary>
public class CompensationAnalysisDto
{
    /// <summary>
    /// Date the analysis was performed
    /// </summary>
    public DateTime AsOfDate { get; set; }

    /// <summary>
    /// Total number of employees included in analysis
    /// </summary>
    public int TotalEmployees { get; set; }

    /// <summary>
    /// Compensation statistics by department
    /// </summary>
    public List<DepartmentCompensationDto> ByDepartment { get; set; } = new();

    /// <summary>
    /// Compensation statistics by job title
    /// </summary>
    public List<JobTitleCompensationDto> ByJobTitle { get; set; } = new();

    /// <summary>
    /// Overall salary statistics
    /// </summary>
    public SalaryStatisticsDto OverallStatistics { get; set; } = new();
}

/// <summary>
/// Compensation statistics for a department
/// </summary>
public class DepartmentCompensationDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public SalaryStatisticsDto Statistics { get; set; } = new();
}

/// <summary>
/// Compensation statistics for a job title
/// </summary>
public class JobTitleCompensationDto
{
    public string JobTitle { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public SalaryStatisticsDto Statistics { get; set; } = new();
}

/// <summary>
/// Statistical measures for salary data
/// All amounts are anonymized and aggregated
/// </summary>
public class SalaryStatisticsDto
{
    /// <summary>
    /// Minimum salary in the group (rounded)
    /// </summary>
    public decimal MinSalary { get; set; }

    /// <summary>
    /// Maximum salary in the group (rounded)
    /// </summary>
    public decimal MaxSalary { get; set; }

    /// <summary>
    /// Average salary in the group (rounded)
    /// </summary>
    public decimal AverageSalary { get; set; }

    /// <summary>
    /// Median salary in the group (rounded)
    /// </summary>
    public decimal MedianSalary { get; set; }

    /// <summary>
    /// 25th percentile salary
    /// </summary>
    public decimal Percentile25 { get; set; }

    /// <summary>
    /// 75th percentile salary
    /// </summary>
    public decimal Percentile75 { get; set; }

    /// <summary>
    /// Currency code (e.g., THB, USD)
    /// </summary>
    public string Currency { get; set; } = "THB";
}

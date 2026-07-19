namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for aggregated HR analytics.
/// </summary>
public class HrAnalyticsDto
{
    public int TotalHeadcount { get; set; }
    public int ActiveEmployees { get; set; }
    public int OnboardingCount { get; set; }
    public decimal TurnoverRate { get; set; }
    public List<DepartmentDistributionDto> DepartmentDistribution { get; set; } = [];
    public List<HireTrendDto> HireTrend { get; set; } = [];
}

/// <summary>
/// DTO for department headcount distribution.
/// </summary>
public class DepartmentDistributionDto
{
    public string Department { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// DTO for hiring trends over time.
/// </summary>
public class HireTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

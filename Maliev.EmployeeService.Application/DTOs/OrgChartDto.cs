using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// Recursive DTO for organizational chart visualization
/// Shows manager and their direct/indirect reports up to specified depth
/// </summary>
public class OrgChartDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PreferredName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public EmploymentStatus EmploymentStatus { get; set; }
    public string WorkLocation { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Depth level in the hierarchy (0 = root/requested employee)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Direct reports of this employee
    /// </summary>
    public List<OrgChartDto> DirectReports { get; set; } = new();

    /// <summary>
    /// Total count of direct reports
    /// </summary>
    public int DirectReportsCount { get; set; }

    /// <summary>
    /// Total count of all reports (direct + indirect)
    /// </summary>
    public int TotalReportsCount { get; set; }
}

using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for team member information visible to managers
/// Excludes compensation and sensitive personal details
/// </summary>
public class TeamMemberDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PreferredName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public EmploymentStatus EmploymentStatus { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public string WorkLocation { get; set; } = string.Empty;
    public string? WorkEmail { get; set; }
    public DateTime StartDate { get; set; }

    // Manager can see if they have direct reports
    public int DirectReportsCount { get; set; }

    // Photo URL if available
    public string? PhotoUrl { get; set; }
}

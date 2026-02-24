namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for Team entity (User Story 5 - Matrix Organizations)
/// </summary>
public class TeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TeamType { get; set; } = string.Empty;
    public Guid? TeamLeadId { get; set; }
    public string? TeamLeadName { get; set; }
    public bool IsActive { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

/// <summary>
/// DTO for Team with detailed member information
/// </summary>
public class TeamDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TeamType { get; set; } = string.Empty;
    public Guid? TeamLeadId { get; set; }
    public string? TeamLeadName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public List<TeamMemberAssignmentDto> Members { get; set; } = new();
}

/// <summary>
/// DTO for team member assignment
/// </summary>
public class TeamMemberAssignmentDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsPrimary { get; set; }
    public string WorkEmail { get; set; } = string.Empty;
}

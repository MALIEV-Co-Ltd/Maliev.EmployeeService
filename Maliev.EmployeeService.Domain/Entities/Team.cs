using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Team entity for matrix organizations
/// Supports cross-functional teams and project-based structures
/// </summary>
public class Team : Entity
{
    /// <summary>
    /// Team name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Team description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Team type (e.g., "Engineering", "Product", "Project")
    /// </summary>
    public string TeamType { get; set; } = string.Empty;

    /// <summary>
    /// Team lead employee ID
    /// </summary>
    public Guid? TeamLeadId { get; set; }

    /// <summary>
    /// Whether team is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to team lead
    /// </summary>
    public Employee? TeamLead { get; set; }

    /// <summary>
    /// Navigation property to team member assignments
    /// </summary>
    public ICollection<EmployeeTeamAssignment> TeamMembers { get; set; } = new List<EmployeeTeamAssignment>();
}

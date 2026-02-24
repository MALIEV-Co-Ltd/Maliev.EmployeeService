using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Team entity representing a group of employees working together.
/// </summary>
public class Team : Entity
{
    /// <summary>
    /// Gets or sets the name of the team.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the team's purpose or project.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the employee who leads the team.
    /// </summary>
    public Guid TeamLeadId { get; set; }

    /// <summary>
    /// Gets or sets the category or type of the team.
    /// </summary>
    public string TeamType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the team is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the employee who is the leader of this team.
    /// </summary>
    public Employee? TeamLead { get; set; }

    /// <summary>
    /// Gets or sets the collection of member assignments for this team.
    /// </summary>
    public ICollection<EmployeeTeamAssignment> TeamMembers { get; set; } = new List<EmployeeTeamAssignment>();
}

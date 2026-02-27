using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Join entity for many-to-many relationship between Employee and Team.
/// Supports matrix organizations where employees can belong to multiple teams.
/// </summary>
public class EmployeeTeamAssignment : Entity
{
    /// <summary>
    /// Gets or sets the unique identifier of the employee.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the team.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the employee's primary team.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets the employee entity.
    /// </summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Gets or sets the team entity.
    /// </summary>
    public Team Team { get; set; } = null!;
}

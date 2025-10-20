using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Join entity for many-to-many relationship between Employee and Team
/// Supports matrix organizations where employees can belong to multiple teams
/// </summary>
public class EmployeeTeamAssignment : Entity
{
    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Team ID
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// Whether this is the employee's primary team
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Navigation property to employee
    /// </summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Navigation property to team
    /// </summary>
    public Team Team { get; set; } = null!;
}

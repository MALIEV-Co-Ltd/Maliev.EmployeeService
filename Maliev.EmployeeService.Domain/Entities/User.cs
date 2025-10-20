using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// User entity for authentication and authorization
/// Links to Employee entity for actual employee data
/// </summary>
public class User : Entity
{
    /// <summary>
    /// Unique username for authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Linked employee ID (foreign key to Employee entity)
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// User's role for RBAC
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last time the user successfully logged in
    /// </summary>
    public DateTime? LastLoginDate { get; set; }

    /// <summary>
    /// Navigation property to linked employee
    /// </summary>
    public Employee? Employee { get; set; }
}

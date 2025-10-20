using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Emergency contact information for an employee
/// </summary>
public class EmergencyContact : Entity
{
    /// <summary>
    /// Employee ID this contact belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Full name of the emergency contact
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Relationship to the employee (e.g., Spouse, Parent, Sibling)
    /// </summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>
    /// Primary phone number
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Email address (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Priority order (1 = primary contact, 2 = secondary, etc.)
    /// </summary>
    public int PriorityOrder { get; set; } = 1;

    /// <summary>
    /// Navigation property to employee
    /// </summary>
    public Employee? Employee { get; set; }
}

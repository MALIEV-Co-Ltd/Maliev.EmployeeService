using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// EmergencyContact entity representing a person to be contacted in case of an emergency for an employee.
/// </summary>
public class EmergencyContact : Entity
{
    /// <summary>
    /// Gets or sets the unique identifier of the employee this contact belongs to.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the contact person.
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relationship of the contact person to the employee (e.g., Spouse, Parent).
    /// </summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary phone number for the contact person.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address for the contact person.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the priority order for contacting this person (e.g., 1 for primary).
    /// </summary>
    public int PriorityOrder { get; set; }

    /// <summary>
    /// Gets or sets the employee entity associated with this emergency contact.
    /// </summary>
    public Employee? Employee { get; set; }
}

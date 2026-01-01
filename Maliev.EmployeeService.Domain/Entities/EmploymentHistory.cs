using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Represents a significant event or change in an employee's career history within the company.
/// </summary>
public class EmploymentHistory : Entity
{
    /// <summary>
    /// Gets or sets the unique identifier of the employee.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the date when the employment event occurred.
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets the type of employment event (e.g., Promotion, Transfer).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the employment event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the employee entity associated with this history record.
    /// </summary>
    public Employee? Employee { get; set; }
}

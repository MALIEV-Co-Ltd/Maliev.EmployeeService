namespace Maliev.EmployeeService.Application.IntegrationEvents;

/// <summary>
/// Integration event published when a new employee is created
/// Used to notify other services (Career, Auth) to set up accounts and permissions
/// </summary>
public class EmployeeCreatedIntegrationEvent
{
    /// <summary>
    /// Unique identifier of the employee
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Employee number (unique identifier in the organization)
    /// </summary>
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the employee
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Work email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Employee's start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Department name
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Job title
    /// </summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

namespace Maliev.EmployeeService.Application.IntegrationEvents;

/// <summary>
/// Integration event published when an employee is transferred to a different department
/// Used to notify other services to update permissions, access, and resources
/// </summary>
public class DepartmentTransferredIntegrationEvent
{
    /// <summary>
    /// Unique identifier of the employee
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Employee number
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
    /// Previous department name
    /// </summary>
    public string FromDepartment { get; set; } = string.Empty;

    /// <summary>
    /// Previous department ID
    /// </summary>
    public Guid FromDepartmentId { get; set; }

    /// <summary>
    /// New department name
    /// </summary>
    public string ToDepartment { get; set; } = string.Empty;

    /// <summary>
    /// New department ID
    /// </summary>
    public Guid ToDepartmentId { get; set; }

    /// <summary>
    /// Effective date of the transfer
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Transfer reason
    /// </summary>
    public string TransferReason { get; set; } = string.Empty;

    /// <summary>
    /// New manager's employee ID (if changed)
    /// </summary>
    public Guid? NewManagerId { get; set; }

    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

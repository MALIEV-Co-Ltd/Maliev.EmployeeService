namespace Maliev.EmployeeService.Application.IntegrationEvents;

/// <summary>
/// Integration event published when employee access should be revoked
/// Used to notify other services (IT, Security) to revoke system access
/// </summary>
public class AccessRevocationRequiredIntegrationEvent
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
    /// Work email address (to be deactivated)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Termination date (last day of employment)
    /// </summary>
    public DateTime TerminationDate { get; set; }

    /// <summary>
    /// Department name
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Job title
    /// </summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// Manager's employee ID (if assigned)
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// Reason for termination
    /// </summary>
    public string? TerminationReason { get; set; }

    /// <summary>
    /// Timestamp when access revocation was triggered
    /// </summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

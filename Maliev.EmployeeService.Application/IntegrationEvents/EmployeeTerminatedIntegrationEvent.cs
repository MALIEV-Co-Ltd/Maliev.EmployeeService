namespace Maliev.EmployeeService.Application.IntegrationEvents;

/// <summary>
/// Integration event published when an employee is terminated
/// Used to notify other services to begin offboarding (revoke access, return assets, etc.)
/// </summary>
public class EmployeeTerminatedIntegrationEvent
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
    /// Termination date (last day of work)
    /// </summary>
    public DateTime TerminationDate { get; set; }

    /// <summary>
    /// Termination reason (Resignation, Termination, Retirement, etc.)
    /// </summary>
    public string TerminationReason { get; set; } = string.Empty;

    /// <summary>
    /// Department at time of termination
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Whether the employee is eligible for rehire
    /// </summary>
    public bool EligibleForRehire { get; set; }

    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

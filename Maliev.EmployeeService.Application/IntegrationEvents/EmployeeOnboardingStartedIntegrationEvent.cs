namespace Maliev.EmployeeService.Application.IntegrationEvents;

/// <summary>
/// Integration event published when employee onboarding process is started
/// Used to notify other services to begin their onboarding tasks (IT provisioning, etc.)
/// </summary>
public class EmployeeOnboardingStartedIntegrationEvent
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
    /// Employee's start date (first day of work)
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
    /// Manager's employee ID (if assigned)
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// Timestamp when onboarding was started
    /// </summary>
    public DateTime OnboardingStartedAt { get; set; } = DateTime.UtcNow;
}

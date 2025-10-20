namespace Maliev.EmployeeService.Application.IntegrationEvents;

/// <summary>
/// Integration event published when onboarding reminder needs to be sent
/// Used to notify notification service to send reminders to managers/HR
/// </summary>
public class OnboardingReminderNeededIntegrationEvent
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
    /// Manager's email address (if assigned)
    /// </summary>
    public string? ManagerEmail { get; set; }

    /// <summary>
    /// Number of completed onboarding items
    /// </summary>
    public int CompletedItems { get; set; }

    /// <summary>
    /// Total number of onboarding items
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Completion percentage
    /// </summary>
    public decimal CompletionPercentage { get; set; }

    /// <summary>
    /// Number of days until start date
    /// </summary>
    public int DaysUntilStartDate { get; set; }

    /// <summary>
    /// Timestamp when reminder was triggered
    /// </summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
}

namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Onboarding workflow status
/// </summary>
public enum OnboardingStatus
{
    /// <summary>
    /// Onboarding has not been started
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Onboarding is in progress
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Onboarding has been completed
    /// </summary>
    Completed = 2
}

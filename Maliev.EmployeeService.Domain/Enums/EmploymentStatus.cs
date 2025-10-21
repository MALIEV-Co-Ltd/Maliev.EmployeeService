namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Current employment status of an employee
/// </summary>
public enum EmploymentStatus
{
    /// <summary>
    /// Currently employed and working
    /// </summary>
    Active = 1,

    /// <summary>
    /// On temporary leave (medical, parental, etc.)
    /// </summary>
    OnLeave = 2,

    /// <summary>
    /// Temporarily suspended (disciplinary or investigation)
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Employment has ended
    /// </summary>
    Terminated = 4
}

namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Status of work visa sponsorship
/// </summary>
public enum SponsorshipStatus
{
    /// <summary>
    /// Sponsorship not required (e.g., citizen, permanent resident)
    /// </summary>
    NotRequired = 1,

    /// <summary>
    /// Sponsorship application pending
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Sponsorship approved
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Sponsorship denied
    /// </summary>
    Denied = 4
}

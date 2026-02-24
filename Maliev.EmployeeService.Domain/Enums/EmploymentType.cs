namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Types of employment arrangements
/// </summary>
public enum EmploymentType
{
    /// <summary>
    /// Full-time permanent employee
    /// </summary>
    FullTime = 1,

    /// <summary>
    /// Part-time employee with reduced hours
    /// </summary>
    PartTime = 2,

    /// <summary>
    /// Fixed-term contractor
    /// </summary>
    Contractor = 3,

    /// <summary>
    /// Intern or trainee
    /// </summary>
    Intern = 4,

    /// <summary>
    /// Consultant or external advisor
    /// </summary>
    Consultant = 5
}

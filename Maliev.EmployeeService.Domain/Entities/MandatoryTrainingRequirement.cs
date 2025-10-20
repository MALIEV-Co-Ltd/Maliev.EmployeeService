using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Mandatory training requirements based on employment type and job role
/// </summary>
public class MandatoryTrainingRequirement : Entity
{
    /// <summary>
    /// Employment type this requirement applies to
    /// </summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>
    /// Job role this requirement applies to (null means applies to all roles)
    /// </summary>
    public string? JobRole { get; set; }

    /// <summary>
    /// Comma-separated list of required course names
    /// </summary>
    public string RequiredCourses { get; set; } = string.Empty;

    /// <summary>
    /// Number of days from employee start date by which training must be completed
    /// </summary>
    public int DeadlineDaysFromStart { get; set; }

    /// <summary>
    /// Indicates if this requirement is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority of this requirement (higher number = higher priority)
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Gets the list of required courses
    /// </summary>
    public IEnumerable<string> GetRequiredCoursesList()
    {
        if (string.IsNullOrWhiteSpace(RequiredCourses))
            return Enumerable.Empty<string>();

        return RequiredCourses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

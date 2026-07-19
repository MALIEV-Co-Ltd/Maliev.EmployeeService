namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Represents performance review ratings on a 1-5 scale
/// Following assumption A-009: Performance review ratings follow a standardized scale
/// </summary>
public enum PerformanceRating
{
    /// <summary>
    /// Unsatisfactory performance - does not meet expectations (1)
    /// </summary>
    Unsatisfactory = 1,

    /// <summary>
    /// Needs improvement - partially meets expectations (2)
    /// </summary>
    NeedsImprovement = 2,

    /// <summary>
    /// Meets expectations - satisfactory performance (3)
    /// </summary>
    MeetsExpectations = 3,

    /// <summary>
    /// Exceeds expectations - strong performance (4)
    /// </summary>
    ExceedsExpectations = 4,

    /// <summary>
    /// Outstanding performance - exceptional contributions (5)
    /// </summary>
    Outstanding = 5
}

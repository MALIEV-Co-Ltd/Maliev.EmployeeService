namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Represents the current status of an employee goal or objective
/// </summary>
public enum GoalStatus
{
    /// <summary>
    /// Goal has been defined but work has not yet started
    /// </summary>
    NotStarted = 1,

    /// <summary>
    /// Goal is actively being worked on
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Goal has been successfully completed
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Goal has been cancelled or is no longer being pursued
    /// </summary>
    Cancelled = 4
}

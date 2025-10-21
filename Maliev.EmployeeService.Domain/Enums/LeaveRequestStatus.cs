namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Status of a leave request
/// </summary>
public enum LeaveRequestStatus
{
    /// <summary>
    /// Leave request is pending manager approval
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Leave request has been approved by manager
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Leave request has been denied by manager
    /// </summary>
    Denied = 3,

    /// <summary>
    /// Leave request has been cancelled by employee
    /// </summary>
    Cancelled = 4
}

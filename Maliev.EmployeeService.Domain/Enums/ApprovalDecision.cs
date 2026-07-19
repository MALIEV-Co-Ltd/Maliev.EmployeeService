namespace Maliev.EmployeeService.Domain.Enums;

/// <summary>
/// Decision made by an approver on a leave request
/// </summary>
public enum ApprovalDecision
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

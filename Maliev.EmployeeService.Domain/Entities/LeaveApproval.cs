using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Approval record for a leave request
/// </summary>
public class LeaveApproval : Entity
{
    public Guid LeaveRequestId { get; set; }
    public Guid ApproverId { get; set; }
    public int ApprovalLevel { get; set; }
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;
    public DateTime? DecisionDate { get; set; }
    public string? Comments { get; set; }

    // Navigation Properties
    public LeaveRequest? LeaveRequest { get; set; }
    public Employee? Approver { get; set; }

    // Computed Properties
    public bool IsPending => Decision == ApprovalDecision.Pending;
    public bool IsApproved => Decision == ApprovalDecision.Approved;
    public bool IsRejected => Decision == ApprovalDecision.Rejected;
}

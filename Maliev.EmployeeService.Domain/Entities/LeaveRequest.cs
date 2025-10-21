using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Leave request submitted by an employee
/// </summary>
public class LeaveRequest : Entity
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    // Approval details
    public Guid? ApproverId { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ApprovalComments { get; set; }

    // Navigation Properties
    public Employee? Employee { get; set; }
    public Employee? Approver { get; set; }

    // Computed Properties
    public bool IsActive => Status == LeaveRequestStatus.Pending;
    public bool CanBeCancelled => Status is LeaveRequestStatus.Pending or LeaveRequestStatus.Approved;
}

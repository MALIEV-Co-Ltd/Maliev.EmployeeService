namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for leave request information
/// </summary>
public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Approval Information (Single Approver)
    public Guid? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ApprovalComments { get; set; }

    // Metadata
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

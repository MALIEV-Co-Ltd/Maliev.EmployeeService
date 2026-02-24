namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for leave approval information
/// </summary>
public class LeaveApprovalDto
{
    public Guid Id { get; set; }
    public Guid LeaveRequestId { get; set; }
    public Guid ApproverId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public int ApprovalLevel { get; set; }
    public string Decision { get; set; } = string.Empty;
    public DateTime? DecisionDate { get; set; }
    public string? Comments { get; set; }
}

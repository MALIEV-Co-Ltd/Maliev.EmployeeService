namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for approving or rejecting a leave request
/// </summary>
public class ApproveRejectLeaveDto
{
    public bool IsApproved { get; set; }
    public string? Comments { get; set; }
}

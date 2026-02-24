using System.ComponentModel.DataAnnotations;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for approving or rejecting a leave request
/// </summary>
public class ApproveRejectLeaveDto
{
    /// <summary>
    /// Indicates if the leave request is approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Comments from the approver.
    /// </summary>
    [StringLength(1000)]
    public string? Comments { get; set; }
}

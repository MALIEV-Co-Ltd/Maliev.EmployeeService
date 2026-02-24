using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to approve or reject a leave request
/// </summary>
public record ApproveRejectLeaveCommand(
    Guid LeaveRequestId,
    Guid ApproverId,
    ApproveRejectLeaveDto DecisionDto
);

/// <summary>
/// Result of approving/rejecting a leave request
/// </summary>
public record ApproveRejectLeaveCommandResult(
    bool Success,
    string? ErrorMessage = null
);

using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to cancel a leave request
/// </summary>
public record CancelLeaveRequestCommand(
    Guid LeaveRequestId,
    Guid EmployeeId,
    CancelLeaveRequestDto CancelDto
);

/// <summary>
/// Result of cancelling a leave request
/// </summary>
public record CancelLeaveRequestCommandResult(
    bool Success,
    string? ErrorMessage = null
);

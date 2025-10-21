using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to submit a new leave request
/// </summary>
public record SubmitLeaveRequestCommand(
    Guid EmployeeId,
    SubmitLeaveRequestDto SubmitDto
);

/// <summary>
/// Result of submitting a leave request
/// </summary>
public record SubmitLeaveRequestCommandResult(
    bool Success,
    Guid? LeaveRequestId = null,
    string? ErrorMessage = null
);

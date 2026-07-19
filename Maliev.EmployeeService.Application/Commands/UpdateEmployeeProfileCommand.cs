using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to update employee profile (User Story 1)
/// Employees can only update limited fields
/// </summary>
public record UpdateEmployeeProfileCommand(
    Guid EmployeeId,
    UpdateEmployeeProfileDto UpdateDto
);

/// <summary>
/// Result of updating employee profile
/// </summary>
public record UpdateEmployeeProfileCommandResult(
    bool Success,
    string? ErrorMessage = null
);

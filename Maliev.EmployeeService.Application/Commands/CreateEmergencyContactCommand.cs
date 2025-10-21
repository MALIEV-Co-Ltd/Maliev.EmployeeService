using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to create a new emergency contact (User Story 1)
/// </summary>
public record CreateEmergencyContactCommand(
    Guid EmployeeId,
    CreateEmergencyContactDto CreateDto
);

/// <summary>
/// Result of creating emergency contact
/// </summary>
public record CreateEmergencyContactCommandResult(
    bool Success,
    Guid? EmergencyContactId = null,
    string? ErrorMessage = null
);

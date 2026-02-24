using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to update an emergency contact (User Story 1)
/// </summary>
public record UpdateEmergencyContactCommand(
    Guid EmergencyContactId,
    UpdateEmergencyContactDto UpdateDto
);

/// <summary>
/// Result of updating emergency contact
/// </summary>
public record UpdateEmergencyContactCommandResult(
    bool Success,
    string? ErrorMessage = null
);

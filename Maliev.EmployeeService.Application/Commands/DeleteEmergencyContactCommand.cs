namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to delete an emergency contact (User Story 1)
/// </summary>
public record DeleteEmergencyContactCommand(Guid EmergencyContactId);

/// <summary>
/// Result of deleting emergency contact
/// </summary>
public record DeleteEmergencyContactCommandResult(
    bool Success,
    string? ErrorMessage = null
);

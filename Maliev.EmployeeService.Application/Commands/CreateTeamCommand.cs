namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to create a new team
/// (User Story 5 - Matrix Organizations)
/// </summary>
public record CreateTeamCommand(
    string Name,
    string? Description,
    string TeamType,
    Guid? TeamLeadId,
    bool IsActive = true
);

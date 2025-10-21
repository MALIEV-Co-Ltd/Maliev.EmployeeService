namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to add a member to a team
/// (User Story 5 - Matrix Organizations)
/// </summary>
public record AddTeamMemberCommand(
    Guid TeamId,
    Guid EmployeeId,
    bool IsPrimary = false
);

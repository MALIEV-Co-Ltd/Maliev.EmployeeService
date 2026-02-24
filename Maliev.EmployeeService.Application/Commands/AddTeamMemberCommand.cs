using System.ComponentModel.DataAnnotations;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to add a member to a team
/// (User Story 5 - Matrix Organizations)
/// </summary>
/// <param name="TeamId">The ID of the team.</param>
/// <param name="EmployeeId">The ID of the employee to add to the team.</param>
/// <param name="IsPrimary">Indicates if this is the employee's primary team.</param>
public record AddTeamMemberCommand(
    [Required]
    Guid TeamId,
    [Required]
    Guid EmployeeId,
    bool IsPrimary = false
);

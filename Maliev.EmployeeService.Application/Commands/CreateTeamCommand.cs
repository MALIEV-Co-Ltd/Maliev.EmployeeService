using System.ComponentModel.DataAnnotations;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to create a new team
/// (User Story 5 - Matrix Organizations)
/// </summary>
/// <param name="Name">The name of the team.</param>
/// <param name="Description">The description of the team.</param>
/// <param name="TeamType">The type of the team.</param>
/// <param name="TeamLeadId">The ID of the team lead.</param>
/// <param name="IsActive">Indicates if the team is active.</param>
public record CreateTeamCommand(
    [Required]
    [StringLength(200)]
    string Name,
    [StringLength(1000)]
    string? Description,
    [Required]
    [StringLength(100)]
    string TeamType,
    Guid? TeamLeadId,
    bool IsActive = true
);

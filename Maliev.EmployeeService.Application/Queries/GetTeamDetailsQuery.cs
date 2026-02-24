namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get detailed information about a specific team including members
/// (User Story 5 - Matrix Organizations)
/// </summary>
public record GetTeamDetailsQuery(Guid TeamId);

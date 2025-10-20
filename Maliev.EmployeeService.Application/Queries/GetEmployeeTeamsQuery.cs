namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get all teams an employee belongs to
/// (User Story 5 - Matrix Organizations)
/// </summary>
public record GetEmployeeTeamsQuery(Guid EmployeeId);

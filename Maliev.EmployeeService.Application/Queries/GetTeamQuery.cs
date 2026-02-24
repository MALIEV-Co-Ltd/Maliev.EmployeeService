using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get direct reports for a manager (User Story 3)
/// Supports pagination for large teams
/// </summary>
public record GetTeamQuery(
    Guid ManagerId,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Response containing paginated list of team members
/// </summary>
public record GetTeamQueryResult(
    List<TeamMemberDto> TeamMembers,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

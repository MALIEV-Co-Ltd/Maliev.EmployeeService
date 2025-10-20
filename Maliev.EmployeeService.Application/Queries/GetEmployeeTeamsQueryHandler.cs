using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetEmployeeTeamsQuery - returns all teams an employee belongs to
/// (User Story 5 - Matrix Organizations)
/// </summary>
public class GetEmployeeTeamsQueryHandler
{
    private readonly ITeamRepository _teamRepository;

    public GetEmployeeTeamsQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<List<TeamDto>> HandleAsync(
        GetEmployeeTeamsQuery query,
        CancellationToken cancellationToken = default)
    {
        var teams = await _teamRepository.GetTeamsByEmployeeAsync(query.EmployeeId, cancellationToken);

        var teamDtos = teams.Select(team => new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            TeamType = team.TeamType,
            TeamLeadId = team.TeamLeadId,
            TeamLeadName = team.TeamLead?.FullName,
            IsActive = team.IsActive,
            MemberCount = team.TeamMembers.Count,
            CreatedDate = team.CreatedDate,
            ModifiedDate = team.ModifiedDate
        }).ToList();

        return teamDtos;
    }
}

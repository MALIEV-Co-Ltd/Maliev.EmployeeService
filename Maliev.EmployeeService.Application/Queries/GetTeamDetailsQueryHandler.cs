using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetTeamDetailsQuery - returns detailed team information with members
/// (User Story 5 - Matrix Organizations)
/// </summary>
public class GetTeamDetailsQueryHandler
{
    private readonly ITeamRepository _teamRepository;

    public GetTeamDetailsQueryHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<TeamDetailsDto?> HandleAsync(
        GetTeamDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var team = await _teamRepository.GetWithMembersAsync(query.TeamId, cancellationToken);

        if (team == null)
        {
            return null;
        }

        var teamDetailsDto = new TeamDetailsDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            TeamType = team.TeamType,
            TeamLeadId = team.TeamLeadId,
            TeamLeadName = team.TeamLead?.FullName,
            IsActive = team.IsActive,
            CreatedDate = team.CreatedDate,
            ModifiedDate = team.ModifiedDate,
            Members = team.TeamMembers.Select(tm => new TeamMemberAssignmentDto
            {
                EmployeeId = tm.EmployeeId,
                EmployeeNumber = tm.Employee.EmployeeNumber,
                FullName = tm.Employee.FullName,
                JobTitle = tm.Employee.JobTitle,
                DepartmentName = tm.Employee.Department?.Name,
                IsPrimary = tm.IsPrimary,
                WorkEmail = tm.Employee.ContactInformation.WorkEmail
            }).ToList()
        };

        return teamDetailsDto;
    }
}

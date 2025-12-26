using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetEmployeeTeamsQuery - returns all teams an employee belongs to
/// (User Story 5 - Matrix Organizations)
/// </summary>
public class GetEmployeeTeamsQueryHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployeeTeamsQueryHandler(
        ITeamRepository teamRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _teamRepository = teamRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<List<TeamDto>> HandleAsync(
        GetEmployeeTeamsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have TeamsView permission for this employee
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.EmployeeId}/teams";
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.TeamsRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view teams for this employee");
        }

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

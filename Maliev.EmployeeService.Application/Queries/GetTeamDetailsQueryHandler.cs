using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetTeamDetailsQuery - returns detailed team information with members
/// (User Story 5 - Matrix Organizations)
/// </summary>
public class GetTeamDetailsQueryHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetTeamDetailsQueryHandler(
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

    public async Task<TeamDetailsDto?> HandleAsync(
        GetTeamDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have TeamsRead permission for this team
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/teams/{query.TeamId}";
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.TeamsRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view this team's details");
        }

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

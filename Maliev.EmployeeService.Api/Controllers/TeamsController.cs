using Asp.Versioning;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Events;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.Aspire.ServiceDefaults.Authorization;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Team management and matrix organization operations (User Story 5).
/// Supports operations for creating, updating, and viewing teams and their members.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/teams")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly GetTeamDetailsQueryHandler _getTeamDetailsHandler;
    private readonly GetEmployeeTeamsQueryHandler _getEmployeeTeamsHandler;
    private readonly CreateTeamCommandHandler _createTeamHandler;
    private readonly AddTeamMemberCommandHandler _addTeamMemberHandler;
    private readonly ITeamRepository _teamRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TeamsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsController"/> class.
    /// </summary>
    /// <param name="getTeamDetailsHandler">The handler for getting team details.</param>
    /// <param name="getEmployeeTeamsHandler">The handler for getting employee teams.</param>
    /// <param name="createTeamHandler">The handler for creating teams.</param>
    /// <param name="addTeamMemberHandler">The handler for adding team members.</param>
    /// <param name="teamRepository">The repository for team data.</param>
    /// <param name="employeeRepository">The repository for employee data.</param>
    /// <param name="currentUserService">The current user service.</param>
    /// <param name="unitOfWork">The unit of work for database transactions.</param>
    /// <param name="eventPublisher">The integration event publisher.</param>
    /// <param name="logger">The logger instance.</param>
    public TeamsController(
        GetTeamDetailsQueryHandler getTeamDetailsHandler,
        GetEmployeeTeamsQueryHandler getEmployeeTeamsHandler,
        CreateTeamCommandHandler createTeamHandler,
        AddTeamMemberCommandHandler addTeamMemberHandler,
        ITeamRepository teamRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<TeamsController> logger)
    {
        _getTeamDetailsHandler = getTeamDetailsHandler;
        _getEmployeeTeamsHandler = getEmployeeTeamsHandler;
        _createTeamHandler = createTeamHandler;
        _addTeamMemberHandler = addTeamMemberHandler;
        _teamRepository = teamRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Get all active teams.
    /// </summary>
    /// <returns>List of all active teams.</returns>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [RequirePermission(EmployeePermissions.TeamsManage)]
    [ProducesResponseType(typeof(IEnumerable<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTeams(CancellationToken cancellationToken = default)
    {
        var teams = await _teamRepository.GetAllActiveAsync(cancellationToken);

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

        return Ok(teamDtos);
    }

    /// <summary>
    /// Get team details with member information.
    /// </summary>
    /// <param name="teamId">Team ID.</param>
    /// <returns>Team details including members.</returns>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{teamId:guid}")]
    [ProducesResponseType(typeof(TeamDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamDetails(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTeamDetailsQuery(teamId);
        var result = await _getTeamDetailsHandler.HandleAsync(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = "Team not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all teams an employee belongs to.
    /// </summary>
    /// <param name="employeeId">Employee ID.</param>
    /// <returns>List of teams for the employee.</returns>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("employee/{employeeId:guid}")]
    [RequirePermission(EmployeePermissions.ProfilesRead, ResourcePathTemplate = "employee/{employeeId}")]
    [ProducesResponseType(typeof(List<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEmployeeTeams(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmployeeTeamsQuery(employeeId);
        var result = await _getEmployeeTeamsHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get teams by type.
    /// </summary>
    /// <param name="teamType">Team type (e.g., "Engineering", "Product", "Project").</param>
    /// <returns>List of teams matching the type.</returns>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("by-type/{teamType}")]
    [ProducesResponseType(typeof(IEnumerable<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamsByType(
        string teamType,
        CancellationToken cancellationToken = default)
    {
        var teams = await _teamRepository.GetByTeamTypeAsync(teamType, cancellationToken);

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

        return Ok(teamDtos);
    }

    /// <summary>
    /// Get teams led by a specific employee.
    /// </summary>
    /// <param name="teamLeadId">Team lead employee ID.</param>
    /// <returns>List of teams led by the employee.</returns>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("by-team-lead/{teamLeadId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamsByTeamLead(
        Guid teamLeadId,
        CancellationToken cancellationToken = default)
    {
        var teams = await _teamRepository.GetByTeamLeadAsync(teamLeadId, cancellationToken);

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

        return Ok(teamDtos);
    }

    /// <summary>
    /// Create a new team.
    /// </summary>
    /// <param name="command">Team creation details.</param>
    /// <returns>Created team ID.</returns>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [RequirePermission(EmployeePermissions.TeamsManage)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTeam(
        [FromBody] CreateTeamCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return BadRequest(new { message = "Team name is required" });
        }

        if (string.IsNullOrWhiteSpace(command.TeamType))
        {
            return BadRequest(new { message = "Team type is required" });
        }

        try
        {
            var teamId = await _createTeamHandler.HandleAsync(command, cancellationToken);

            _logger.LogInformation("Team {TeamId} created by user {UserId}",
                teamId, _currentUserService.PrincipalId);

            return CreatedAtAction(
                nameof(GetTeamDetails),
                new { teamId },
                new { teamId, message = "Team created successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create team");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Add a member to a team.
    /// </summary>
    /// <param name="teamId">Team ID.</param>
    /// <param name="request">Member assignment details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success result.</returns>
    [HttpPost("{teamId:guid}/members")]
    [RequirePermission(EmployeePermissions.TeamsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTeamMember(
        Guid teamId,
        [FromBody] AddTeamMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new AddTeamMemberCommand(
            teamId,
            request.EmployeeId,
            request.IsPrimary);

        try
        {
            await _addTeamMemberHandler.HandleAsync(command, cancellationToken);

            _logger.LogInformation("Employee {EmployeeId} added to team {TeamId} by user {UserId}",
                request.EmployeeId, teamId, _currentUserService.PrincipalId);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to add team member");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a member from a team.
    /// </summary>
    /// <param name="teamId">Team ID.</param>
    /// <param name="employeeId">Employee ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success result.</returns>
    [HttpDelete("{teamId:guid}/members/{employeeId:guid}")]
    [RequirePermission(EmployeePermissions.TeamsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveTeamMember(
        Guid teamId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var team = await _teamRepository.GetWithMembersAsync(teamId, cancellationToken);
        if (team == null)
        {
            return NotFound(new { message = "Team not found" });
        }

        var assignment = team.TeamMembers.FirstOrDefault(tm => tm.EmployeeId == employeeId);
        if (assignment == null)
        {
            return NotFound(new { message = "Employee is not a member of this team" });
        }

        // Get employee details for event
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee == null)
        {
            return NotFound(new { message = "Employee not found" });
        }

        team.TeamMembers.Remove(assignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish TeamMemberRemovedEvent
        var teamMemberRemovedEvent = new TeamMemberRemovedEvent
        {
            TeamId = team.Id,
            TeamName = team.Name,
            EmployeeId = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            EmployeeName = employee.FullName,
            RemovedAt = DateTime.UtcNow,
            RemovedBy = _currentUserService.PrincipalId ?? Guid.Empty
        };

        await _eventPublisher.PublishAsync(teamMemberRemovedEvent, cancellationToken);

        _logger.LogInformation("Employee {EmployeeId} removed from team {TeamId} by user {UserId}",
            employeeId, teamId, _currentUserService.PrincipalId);

        return NoContent();
    }

    /// <summary>
    /// Update team information.
    /// </summary>
    /// <param name="teamId">Team ID.</param>
    /// <param name="request">Updated team information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success result.</returns>
    [HttpPut("{teamId:guid}")]
    [RequirePermission(EmployeePermissions.TeamsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeam(
        Guid teamId,
        [FromBody] UpdateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
        if (team == null)
        {
            return NotFound(new { message = "Team not found" });
        }

        // Track changes for event
        var changes = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(request.Name) && team.Name != request.Name)
        {
            changes["Name"] = new { Old = team.Name, New = request.Name };
            team.Name = request.Name;
        }

        if (request.Description != null && team.Description != request.Description)
        {
            changes["Description"] = new { Old = team.Description, New = request.Description };
            team.Description = request.Description;
        }

        if (!string.IsNullOrWhiteSpace(request.TeamType) && team.TeamType != request.TeamType)
        {
            changes["TeamType"] = new { Old = team.TeamType, New = request.TeamType };
            team.TeamType = request.TeamType;
        }

        if (request.TeamLeadId.HasValue && team.TeamLeadId != request.TeamLeadId.Value)
        {
            changes["TeamLeadId"] = new { Old = team.TeamLeadId, New = request.TeamLeadId.Value };
            team.TeamLeadId = request.TeamLeadId.Value;
        }

        if (request.IsActive.HasValue && team.IsActive != request.IsActive.Value)
        {
            changes["IsActive"] = new { Old = team.IsActive, New = request.IsActive.Value };
            team.IsActive = request.IsActive.Value;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish TeamUpdatedEvent if there were changes
        if (changes.Any())
        {
            var teamUpdatedEvent = new TeamUpdatedEvent
            {
                TeamId = team.Id,
                TeamName = team.Name,
                Changes = changes,
                UpdatedBy = _currentUserService.PrincipalId ?? Guid.Empty,
                UpdatedAt = DateTime.UtcNow
            };

            await _eventPublisher.PublishAsync(teamUpdatedEvent, cancellationToken);
        }

        _logger.LogInformation("Team {TeamId} updated by user {PrincipalId}",
            team.Id, _currentUserService.PrincipalId);

        return NoContent();
    }

    /// <summary>
    /// Deactivate a team.
    /// </summary>
    /// <param name="teamId">Team ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success result.</returns>
    [HttpDelete("{teamId:guid}")]
    [RequirePermission(EmployeePermissions.TeamsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateTeam(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
        if (team == null)
        {
            return NotFound(new { message = "Team not found" });
        }

        var wasActive = team.IsActive;
        team.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish TeamUpdatedEvent if team was active
        if (wasActive)
        {
            var changes = new Dictionary<string, object?>
            {
                ["IsActive"] = new { Old = true, New = false }
            };

            var teamUpdatedEvent = new TeamUpdatedEvent
            {
                TeamId = team.Id,
                TeamName = team.Name,
                TeamType = team.TeamType,
                Description = team.Description,
                TeamLeadId = team.TeamLeadId,
                IsActive = team.IsActive,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = _currentUserService.PrincipalId ?? Guid.Empty,
                Changes = changes
            };

            await _eventPublisher.PublishAsync(teamUpdatedEvent, cancellationToken);
        }

        _logger.LogInformation("Team {TeamId} deactivated by user {PrincipalId}",
            teamId, _currentUserService.PrincipalId);

        return NoContent();
    }
}

/// <summary>
/// Request model for adding a team member.
/// </summary>
/// <param name="EmployeeId">The unique identifier of the employee to add.</param>
/// <param name="IsPrimary">Indicates if this is the employee's primary team.</param>
public record AddTeamMemberRequest(Guid EmployeeId, bool IsPrimary = false);

/// <summary>
/// Request model for updating a team.
/// </summary>
/// <param name="Name">The updated name of the team.</param>
/// <param name="Description">The updated description of the team.</param>
/// <param name="TeamType">The updated type of the team.</param>
/// <param name="TeamLeadId">The unique identifier of the new team lead.</param>
/// <param name="IsActive">The updated active status of the team.</param>
public record UpdateTeamRequest(
    string? Name = null,
    string? Description = null,
    string? TeamType = null,
    Guid? TeamLeadId = null,
    bool? IsActive = null);
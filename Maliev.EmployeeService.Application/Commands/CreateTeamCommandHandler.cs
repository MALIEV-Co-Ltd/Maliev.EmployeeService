using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.MessagingContracts.Generated;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for CreateTeamCommand
/// (User Story 5 - Matrix Organizations)
/// </summary>
public class CreateTeamCommandHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;

    public CreateTeamCommandHandler(
        ITeamRepository teamRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _teamRepository = teamRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> HandleAsync(
        CreateTeamCommand command,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have TeamsManage permission
        var principalId = _currentUserService.PrincipalIdentifier;
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.TeamsManage, "employee/teams", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage teams");
        }
        // Validate team lead exists if provided
        if (command.TeamLeadId.HasValue)
        {
            var teamLead = await _employeeRepository.GetByIdAsync(command.TeamLeadId.Value, cancellationToken);
            if (teamLead == null)
            {
                throw new InvalidOperationException($"Team lead with ID {command.TeamLeadId.Value} not found");
            }
        }

        var team = new Team
        {
            Name = command.Name,
            Description = command.Description,
            TeamType = command.TeamType,
            TeamLeadId = command.TeamLeadId ?? Guid.Empty,
            IsActive = command.IsActive
        };

        await _teamRepository.AddAsync(team, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish TeamCreatedEvent
        var teamCreatedEvent = new TeamCreatedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(TeamCreatedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "EmployeeService",
            ConsumedBy: Array.Empty<string>(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new TeamCreatedEventPayload(
                TeamId: team.Id,
                TeamName: team.Name,
                TeamType: team.TeamType,
                Description: team.Description,
                TeamLeadId: team.TeamLeadId,
                IsActive: team.IsActive,
                CreatedAt: DateTimeOffset.UtcNow,
                CreatedBy: _currentUserService.PrincipalId ?? Guid.Empty
            )
        );

        await _eventPublisher.PublishAsync(teamCreatedEvent, cancellationToken);

        return team.Id;
    }
}

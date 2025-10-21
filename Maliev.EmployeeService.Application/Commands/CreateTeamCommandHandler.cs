using Maliev.EmployeeService.Application.Events;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;

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

    public CreateTeamCommandHandler(
        ITeamRepository teamRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ICurrentUserService currentUserService)
    {
        _teamRepository = teamRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> HandleAsync(
        CreateTeamCommand command,
        CancellationToken cancellationToken = default)
    {
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
            TeamLeadId = command.TeamLeadId,
            IsActive = command.IsActive
        };

        await _teamRepository.AddAsync(team, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish TeamCreatedEvent
        var teamCreatedEvent = new TeamCreatedEvent
        {
            TeamId = team.Id,
            TeamName = team.Name,
            TeamType = team.TeamType,
            Description = team.Description,
            TeamLeadId = team.TeamLeadId,
            IsActive = team.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.EmployeeId ?? Guid.Empty
        };

        await _eventPublisher.PublishAsync(teamCreatedEvent, cancellationToken);

        return team.Id;
    }
}

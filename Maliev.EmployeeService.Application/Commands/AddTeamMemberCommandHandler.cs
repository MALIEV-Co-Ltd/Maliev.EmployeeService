using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.MessagingContracts.Contracts.Employee;
using Maliev.MessagingContracts.Generated;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for AddTeamMemberCommand
/// (User Story 5 - Matrix Organizations)
/// </summary>
public class AddTeamMemberCommandHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRepository<EmployeeTeamAssignment> _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUserService _currentUserService;

    public AddTeamMemberCommandHandler(
        ITeamRepository teamRepository,
        IEmployeeRepository employeeRepository,
        IRepository<EmployeeTeamAssignment> assignmentRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ICurrentUserService currentUserService)
    {
        _teamRepository = teamRepository;
        _employeeRepository = employeeRepository;
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _currentUserService = currentUserService;
    }

    public async Task HandleAsync(
        AddTeamMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate team exists
        var team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken);
        if (team == null)
        {
            throw new InvalidOperationException($"Team with ID {command.TeamId} not found");
        }

        // Validate employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {command.EmployeeId} not found");
        }

        // Check if already a member
        var isAlreadyMember = await _teamRepository.IsEmployeeMemberAsync(
            command.EmployeeId,
            command.TeamId,
            cancellationToken);

        if (isAlreadyMember)
        {
            throw new InvalidOperationException($"Employee {command.EmployeeId} is already a member of team {command.TeamId}");
        }

        var assignment = new EmployeeTeamAssignment
        {
            EmployeeId = command.EmployeeId,
            TeamId = command.TeamId,
            IsPrimary = command.IsPrimary
        };

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish TeamMemberAddedEvent
        var teamMemberAddedEvent = new TeamMemberAddedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(TeamMemberAddedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "EmployeeService",
            ConsumedBy: Array.Empty<string>(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new TeamMemberAddedEventPayload(
                TeamId: team.Id,
                TeamName: team.Name,
                EmployeeId: employee.Id,
                EmployeeNumber: employee.EmployeeNumber,
                EmployeeName: employee.FullName,
                IsPrimary: command.IsPrimary,
                AddedAt: DateTimeOffset.UtcNow,
                AddedBy: _currentUserService.PrincipalId ?? Guid.Empty
            )
        );

        await _eventPublisher.PublishAsync(teamMemberAddedEvent, cancellationToken);
    }
}

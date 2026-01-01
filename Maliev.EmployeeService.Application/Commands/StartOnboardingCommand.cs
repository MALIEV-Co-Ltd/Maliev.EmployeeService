using Maliev.EmployeeService.Application.Interfaces;
using Maliev.MessagingContracts.Generated;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to start onboarding workflow for an employee
/// </summary>
public class StartOnboardingCommand
{
    public Guid EmployeeId { get; set; }
}

/// <summary>
/// Handler for starting employee onboarding workflow
/// Notifies external services through integration event
/// </summary>
public class StartOnboardingCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIamServiceClient _iamClient;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<StartOnboardingCommandHandler> _logger;

    public StartOnboardingCommandHandler(
        IEmployeeRepository employeeRepository,
        IEventPublisher eventPublisher,
        IIamServiceClient iamClient,
        ICurrentUserService currentUserService,
        ILogger<StartOnboardingCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _eventPublisher = eventPublisher;
        _iamClient = iamClient;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(StartOnboardingCommand command, CancellationToken cancellationToken = default)
    {
        // Authorization check
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ProfilesUpdate, "employee/profiles", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage onboarding");
        }

        // Get employee details
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee {command.EmployeeId} not found");
        }

        // Publish EmployeeCreatedEvent to trigger onboarding in Lifecycle Service (Phase 3 - T125)
        var payload = new EmployeeCreatedEventPayload(
            EmployeeId: employee.Id,
            EmployeeNumber: employee.EmployeeNumber,
            StartDate: employee.StartDate,
            DepartmentId: employee.DepartmentId ?? Guid.Empty,
            PositionId: null,
            ManagerId: employee.ManagerId
        );

        var integrationEvent = new EmployeeCreatedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "EmployeeCreated",
            MessageType: MessageType.Event,
            MessageVersion: "1.0",
            PublishedBy: "EmployeeService",
            ConsumedBy: Array.Empty<string>(),
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: payload
        );

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "Published EmployeeCreatedEvent for onboarding start: {EmployeeId}",
            employee.Id);

        return employee.Id;
    }
}

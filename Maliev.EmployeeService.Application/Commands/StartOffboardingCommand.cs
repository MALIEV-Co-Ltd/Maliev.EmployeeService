using Maliev.EmployeeService.Application.Interfaces;
using Maliev.MessagingContracts.Generated;
// using Maliev.EmployeeService.Domain.IntegrationEvents; // Removed
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to start offboarding workflow for an employee
/// </summary>
public class StartOffboardingCommand
{
    public Guid EmployeeId { get; set; }
    public DateTime TerminationDate { get; set; }
    public string TerminationReason { get; set; } = string.Empty;
    public bool EligibleForRehire { get; set; }
}

/// <summary>
/// Handler for starting employee offboarding workflow
/// Sets termination date and publishes integration event
/// </summary>
public class StartOffboardingCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<StartOffboardingCommandHandler> _logger;

    public StartOffboardingCommandHandler(
        IEmployeeRepository employeeRepository,
        IEventPublisher eventPublisher,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ILogger<StartOffboardingCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _eventPublisher = eventPublisher;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(StartOffboardingCommand command, CancellationToken cancellationToken = default)
    {
        // Authorization check
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ProfilesDelete, "employee/profiles", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage offboarding");
        }

        _logger.LogInformation(
            "Starting termination for employee {EmployeeId}, termination date {TerminationDate}",
            command.EmployeeId,
            command.TerminationDate);

        // Get employee details
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee {command.EmployeeId} not found");
        }

        // Update employee termination date and status
        employee.TerminationDate = command.TerminationDate;
        employee.EmploymentStatus = EmploymentStatus.Terminated;
        employee.ModifiedDate = DateTime.UtcNow;

        _employeeRepository.Update(employee);

        // Publish integration event (Phase 3 - T126)
        var integrationEvent = new EmployeeTerminatedEvent
        {
            Payload = new EmployeeTerminatedEventPayload
            {
                EmployeeId = employee.Id,
                EmployeeNumber = employee.EmployeeNumber,
                TerminationDate = command.TerminationDate
            }
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "Published EmployeeTerminatedEvent for employee {EmployeeId}",
            employee.Id);

        return employee.Id;
    }
}
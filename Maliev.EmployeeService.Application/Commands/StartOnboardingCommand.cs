using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.IntegrationEvents;
using Maliev.EmployeeService.Domain.IntegrationEvents;
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

        // Publish EmployeeCreatedIntegrationEvent to trigger onboarding in Lifecycle Service
        var integrationEvent = new EmployeeCreatedIntegrationEvent(
            employee.Id,
            employee.EmployeeNumber,
            employee.FullName,
            employee.ContactInformation.WorkEmail,
            employee.StartDate,
            employee.DepartmentId ?? Guid.Empty,
            employee.ManagerId,
            employee.JobTitle ?? "Employee"
        );

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "Published EmployeeCreatedIntegrationEvent for onboarding start: {EmployeeId}",
            employee.Id);

        return employee.Id;
    }
}
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.IntegrationEvents;
using Maliev.EmployeeService.Application.Services;
using Microsoft.Extensions.Logging;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to start onboarding workflow for a new employee
/// </summary>
public class StartOnboardingCommand
{
    public Guid EmployeeId { get; set; }
}

/// <summary>
/// Handler for starting employee onboarding workflow
/// Generates checklist from template and publishes integration event
/// </summary>
public class StartOnboardingCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly OnboardingTemplateService _templateService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<StartOnboardingCommandHandler> _logger;

    public StartOnboardingCommandHandler(
        IEmployeeRepository employeeRepository,
        IOnboardingRepository onboardingRepository,
        OnboardingTemplateService templateService,
        IEventPublisher eventPublisher,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ILogger<StartOnboardingCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _onboardingRepository = onboardingRepository;
        _templateService = templateService;
        _eventPublisher = eventPublisher;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(StartOnboardingCommand command, CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have OnboardingManage permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.OnboardingManage, "employee/onboarding", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage onboarding");
        }

        _logger.LogInformation("Starting onboarding workflow for employee {EmployeeId}", command.EmployeeId);

        // Get employee details
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee {command.EmployeeId} not found");
        }

        // Generate checklist from template
        var checklist = _templateService.GenerateChecklist(
            employee.Id,
            employee.JobTitle ?? "Employee",
            employee.StartDate);

        // Save checklist
        await _onboardingRepository.CreateChecklistAsync(checklist, cancellationToken);

        _logger.LogInformation(
            "Created {Count} onboarding checklist items for employee {EmployeeId}",
            checklist.Count,
            employee.Id);

        // Publish integration event
        var integrationEvent = new EmployeeOnboardingStartedIntegrationEvent
        {
            EmployeeId = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = $"{employee.LegalName.FirstName} {employee.LegalName.LastName}",
            Email = employee.ContactInformation.WorkEmail,
            StartDate = employee.StartDate,
            Department = employee.Department?.Name ?? "Unknown",
            JobTitle = employee.JobTitle ?? "Employee",
            ManagerId = employee.ManagerId,
            OnboardingStartedAt = DateTime.UtcNow
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "Published EmployeeOnboardingStartedIntegrationEvent for employee {EmployeeId}",
            employee.Id);

        return employee.Id;
    }
}

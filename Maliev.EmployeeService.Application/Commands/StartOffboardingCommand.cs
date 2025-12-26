using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.IntegrationEvents;
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
/// Sets termination date, creates offboarding checklist, and publishes integration event
/// </summary>
public class StartOffboardingCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOffboardingRepository _offboardingRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<StartOffboardingCommandHandler> _logger;

    public StartOffboardingCommandHandler(
        IEmployeeRepository employeeRepository,
        IOffboardingRepository offboardingRepository,
        IEventPublisher eventPublisher,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ILogger<StartOffboardingCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _offboardingRepository = offboardingRepository;
        _eventPublisher = eventPublisher;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(StartOffboardingCommand command, CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have OnboardingManage permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.OnboardingManage, "employee/onboarding", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage offboarding");
        }

        _logger.LogInformation(
            "Starting offboarding workflow for employee {EmployeeId}, termination date {TerminationDate}",
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

        // Repository will handle the update through SaveChanges

        // Generate standard offboarding checklist
        var checklist = GenerateOffboardingChecklist(employee.Id, command.TerminationDate);

        // Save checklist
        await _offboardingRepository.CreateChecklistAsync(checklist, cancellationToken);

        _logger.LogInformation(
            "Created {Count} offboarding checklist items for employee {EmployeeId}",
            checklist.Count,
            employee.Id);

        // Publish integration event
        var integrationEvent = new EmployeeTerminatedIntegrationEvent
        {
            EmployeeId = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = $"{employee.LegalName.FirstName} {employee.LegalName.LastName}",
            Email = employee.ContactInformation.WorkEmail,
            TerminationDate = command.TerminationDate,
            TerminationReason = command.TerminationReason,
            Department = employee.Department?.Name ?? "Unknown",
            EligibleForRehire = command.EligibleForRehire,
            EventTimestamp = DateTime.UtcNow
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "Published EmployeeTerminatedIntegrationEvent for employee {EmployeeId}",
            employee.Id);

        return employee.Id;
    }

    private List<OffboardingChecklist> GenerateOffboardingChecklist(Guid employeeId, DateTime terminationDate)
    {
        var items = new[]
        {
            new { Description = "Conduct exit interview", ResponsibleParty = ResponsibleParty.HR, DaysBeforeEnd = -7, DisplayOrder = 1, BlocksPaycheck = false },
            new { Description = "Return company laptop and equipment", ResponsibleParty = ResponsibleParty.IT, DaysBeforeEnd = 0, DisplayOrder = 2, BlocksPaycheck = true },
            new { Description = "Return building access card and keys", ResponsibleParty = ResponsibleParty.Facilities, DaysBeforeEnd = 0, DisplayOrder = 3, BlocksPaycheck = true },
            new { Description = "Complete knowledge transfer documentation", ResponsibleParty = ResponsibleParty.Manager, DaysBeforeEnd = -3, DisplayOrder = 4, BlocksPaycheck = false },
            new { Description = "Revoke email and system access", ResponsibleParty = ResponsibleParty.IT, DaysBeforeEnd = 0, DisplayOrder = 5, BlocksPaycheck = false },
            new { Description = "Process final expense reimbursements", ResponsibleParty = ResponsibleParty.HR, DaysBeforeEnd = 3, DisplayOrder = 6, BlocksPaycheck = true },
            new { Description = "Clear outstanding company credit card charges", ResponsibleParty = ResponsibleParty.HR, DaysBeforeEnd = 3, DisplayOrder = 7, BlocksPaycheck = true },
            new { Description = "Return company uniform or safety equipment", ResponsibleParty = ResponsibleParty.Facilities, DaysBeforeEnd = 0, DisplayOrder = 8, BlocksPaycheck = true },
            new { Description = "Complete final timesheet approval", ResponsibleParty = ResponsibleParty.Manager, DaysBeforeEnd = 1, DisplayOrder = 9, BlocksPaycheck = true },
            new { Description = "Process final paycheck and PTO payout", ResponsibleParty = ResponsibleParty.HR, DaysBeforeEnd = 7, DisplayOrder = 10, BlocksPaycheck = false },
            new { Description = "Provide benefits continuation (COBRA) information", ResponsibleParty = ResponsibleParty.HR, DaysBeforeEnd = 3, DisplayOrder = 11, BlocksPaycheck = false },
            new { Description = "Archive employee records and documents", ResponsibleParty = ResponsibleParty.HR, DaysBeforeEnd = 14, DisplayOrder = 12, BlocksPaycheck = false }
        };

        var checklist = new List<OffboardingChecklist>();
        foreach (var item in items)
        {
            checklist.Add(new OffboardingChecklist
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                ItemDescription = item.Description,
                ResponsibleParty = item.ResponsibleParty,
                DueDate = terminationDate.AddDays(item.DaysBeforeEnd),
                CompletionStatus = false,
                BlocksFinalPaycheck = item.BlocksPaycheck,
                DisplayOrder = item.DisplayOrder,
                CreatedDate = DateTime.UtcNow
            });
        }

        return checklist;
    }
}

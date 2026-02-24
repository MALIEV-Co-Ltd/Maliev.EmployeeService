using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.MessagingContracts.Generated;
using Maliev.MessagingContracts.Contracts.Employee;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

public class AutoProvisionEmployeeCommandHandler
{
    private readonly IEmployeeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIAMClient _iamClient;
    private readonly ILogger<AutoProvisionEmployeeCommandHandler> _logger;

    public AutoProvisionEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IIAMClient iamClient,
        ILogger<AutoProvisionEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _iamClient = iamClient;
        _logger = logger;
    }

    public async Task<AutoProvisionEmployeeDto> HandleAsync(AutoProvisionEmployeeCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("Employee {Email} already exists, returning existing details.", request.Email);
            return new AutoProvisionEmployeeDto
            {
                Id = existing.Id,
                PrincipalId = existing.PrincipalId,
                Email = existing.ContactInformation.WorkEmail,
                EmployeeNumber = existing.EmployeeNumber,
                IsNewEmployee = false
            };
        }

        // Create IAM Principal first (same pattern as CreateEmployeeCommand)
        Guid principalId;
        try
        {
            var principalRequest = new CreatePrincipalRequest
            {
                Email = request.Email,
                LinkedService = "EmployeeService",
                LinkedEntityId = null
            };

            var principalResponse = await _iamClient.CreatePrincipalAsync(principalRequest, cancellationToken);
            principalId = principalResponse.PrincipalId;

            _logger.LogInformation("Created IAM principal {PrincipalId} for auto-provisioned employee {Email}", principalId, request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create principal in IAM for auto-provisioned employee {Email}", request.Email);
            throw new InvalidOperationException("Failed to create employee identity in IAM. Please ensure identity service is available.", ex);
        }

        // Create new employee with the IAM PrincipalId
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            EmployeeNumber = $"TEMP-{Guid.NewGuid():N}",
            LegalName = new LegalName
            {
                FirstName = request.FirstName,
                LastName = request.LastName
            },
            PreferredName = request.FirstName,
            ContactInformation = new ContactInformation
            {
                WorkEmail = request.Email
            },
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        await _repository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Auto-provisioned new employee {Email} with ID {Id} and PrincipalId {PrincipalId}", request.Email, employee.Id, employee.PrincipalId);

        // Publish EmployeeCreatedEvent
        var @event = new EmployeeCreatedEvent(
            Guid.NewGuid(),
            "EmployeeCreatedEvent",
            MessageType.Event,
            "1.0.0",
            "EmployeeService",
            ["IAMService"],
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            false,
            new EmployeeCreatedEventPayload(
                employee.Id,
                employee.EmployeeNumber,
                employee.PrincipalId,
                employee.ContactInformation.WorkEmail,
                $"{employee.LegalName.FirstName} {employee.LegalName.LastName}",
                employee.StartDate,
                Guid.Empty,
                null,
                null
            )
        );

        await _eventPublisher.PublishAsync(@event, cancellationToken);

        return new AutoProvisionEmployeeDto
        {
            Id = employee.Id,
            PrincipalId = employee.PrincipalId,
            Email = employee.ContactInformation.WorkEmail,
            EmployeeNumber = employee.EmployeeNumber,
            IsNewEmployee = true
        };
    }
}

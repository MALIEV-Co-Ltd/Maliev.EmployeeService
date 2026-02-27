using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.MessagingContracts.Contracts.Employee;
using Maliev.MessagingContracts;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

public class AutoProvisionEmployeeCommandHandler
{
    private readonly IEmployeeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<AutoProvisionEmployeeCommandHandler> _logger;

    public AutoProvisionEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<AutoProvisionEmployeeCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
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

        // Create new employee
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(), // Will be linked to IAM later
            EmployeeNumber = $"TEMP-{Guid.NewGuid():N}", // Temporary number, to be replaced by a proper sequence
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

        _logger.LogInformation("Auto-provisioned new employee {Email} with ID {Id}", request.Email, employee.Id);

        // Publish EmployeeCreatedEvent
        var @event = new EmployeeCreatedEvent(
            Guid.NewGuid(),
            "EmployeeCreatedEvent",
            Maliev.MessagingContracts.MessageType.Event,
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
                Guid.Empty, // DepartmentId not available in auto-provision
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

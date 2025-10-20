using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to record a compensation change for an employee
/// </summary>
public record RecordCompensationChangeCommand(
    Guid EmployeeId,
    RecordCompensationChangeDto CompensationDto
);

/// <summary>
/// Result of recording a compensation change
/// </summary>
public record RecordCompensationChangeCommandResult(
    bool Success,
    Guid? CompensationRecordId = null,
    string? ErrorMessage = null
);

/// <summary>
/// Handler for RecordCompensationChangeCommand
/// Handles salary encryption (via EncryptionInterceptor), audit logging, and integration events
/// </summary>
public class RecordCompensationChangeCommandHandler
{
    private readonly ICompensationRepository _compensationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public RecordCompensationChangeCommandHandler(
        ICompensationRepository compensationRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork)
    {
        _compensationRepository = compensationRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecordCompensationChangeCommandResult> HandleAsync(
        RecordCompensationChangeCommand command,
        CancellationToken cancellationToken = default)
    {
        // Verify employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return new RecordCompensationChangeCommandResult(false, null, "Employee not found");
        }

        // Validate salary amount
        if (command.CompensationDto.SalaryAmount <= 0)
        {
            return new RecordCompensationChangeCommandResult(false, null, "Salary amount must be greater than zero");
        }

        // Validate effective date
        if (command.CompensationDto.EffectiveDate < employee.StartDate)
        {
            return new RecordCompensationChangeCommandResult(
                false, null,
                "Effective date cannot be before employee start date");
        }

        // Validate change reason is provided
        if (string.IsNullOrWhiteSpace(command.CompensationDto.ChangeReason))
        {
            return new RecordCompensationChangeCommandResult(false, null, "Change reason is required");
        }

        // Create compensation record
        // Note: SalaryAmount will be encrypted by EncryptionInterceptor when saved
        var compensationRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            SalaryAmount = command.CompensationDto.SalaryAmount.ToString("F2"), // Store as string for encryption
            Currency = command.CompensationDto.Currency,
            EffectiveDate = command.CompensationDto.EffectiveDate,
            ChangeReason = command.CompensationDto.ChangeReason,
            BonusStructure = command.CompensationDto.BonusStructure,
            CommissionStructure = command.CompensationDto.CommissionStructure,
            CreatedBy = _currentUserService.EmployeeId,
            CreatedDate = DateTime.UtcNow
        };

        await _compensationRepository.CreateAsync(compensationRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish integration event for Payroll Service
        // TODO: Define CompensationChangedEvent in Application/Events
        // await _eventPublisher.PublishAsync(new CompensationChangedEvent
        // {
        //     EmployeeId = command.EmployeeId,
        //     CompensationRecordId = compensationRecord.Id,
        //     EffectiveDate = compensationRecord.EffectiveDate,
        //     Currency = compensationRecord.Currency,
        //     ChangedBy = _currentUserService.EmployeeId ?? Guid.Empty,
        //     ChangedDate = DateTime.UtcNow
        // }, cancellationToken);

        return new RecordCompensationChangeCommandResult(true, compensationRecord.Id);
    }
}

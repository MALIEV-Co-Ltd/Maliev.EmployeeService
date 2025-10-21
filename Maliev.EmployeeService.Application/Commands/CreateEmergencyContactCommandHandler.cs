using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for CreateEmergencyContactCommand
/// </summary>
public class CreateEmergencyContactCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmergencyContactRepository _emergencyContactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEmergencyContactCommandHandler(
        IEmployeeRepository employeeRepository,
        IEmergencyContactRepository emergencyContactRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _emergencyContactRepository = emergencyContactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateEmergencyContactCommandResult> HandleAsync(
        CreateEmergencyContactCommand command,
        CancellationToken cancellationToken = default)
    {
        // Verify employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return new CreateEmergencyContactCommandResult(false, null, "Employee not found");
        }

        // Create new emergency contact
        var emergencyContact = new EmergencyContact
        {
            Id = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            ContactName = command.CreateDto.ContactName,
            Relationship = command.CreateDto.Relationship,
            PhoneNumber = command.CreateDto.PhoneNumber,
            Email = command.CreateDto.Email,
            PriorityOrder = command.CreateDto.PriorityOrder,
            CreatedDate = DateTime.UtcNow
        };

        await _emergencyContactRepository.AddAsync(emergencyContact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateEmergencyContactCommandResult(true, emergencyContact.Id);
    }
}

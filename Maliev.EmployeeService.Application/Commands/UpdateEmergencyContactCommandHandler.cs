using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for UpdateEmergencyContactCommand
/// </summary>
public class UpdateEmergencyContactCommandHandler
{
    private readonly IEmergencyContactRepository _emergencyContactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEmergencyContactCommandHandler(
        IEmergencyContactRepository emergencyContactRepository,
        IUnitOfWork unitOfWork)
    {
        _emergencyContactRepository = emergencyContactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateEmergencyContactCommandResult> HandleAsync(
        UpdateEmergencyContactCommand command,
        CancellationToken cancellationToken = default)
    {
        var emergencyContact = await _emergencyContactRepository.GetByIdAsync(
            command.EmergencyContactId,
            cancellationToken);

        if (emergencyContact == null)
        {
            return new UpdateEmergencyContactCommandResult(false, "Emergency contact not found");
        }

        // Update fields
        emergencyContact.ContactName = command.UpdateDto.ContactName;
        emergencyContact.Relationship = command.UpdateDto.Relationship;
        emergencyContact.PhoneNumber = command.UpdateDto.PhoneNumber;
        emergencyContact.Email = command.UpdateDto.Email;
        emergencyContact.PriorityOrder = command.UpdateDto.PriorityOrder;

        _emergencyContactRepository.Update(emergencyContact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateEmergencyContactCommandResult(true);
    }
}

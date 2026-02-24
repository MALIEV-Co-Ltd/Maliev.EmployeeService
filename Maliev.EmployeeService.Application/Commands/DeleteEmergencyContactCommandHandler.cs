using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for DeleteEmergencyContactCommand
/// </summary>
public class DeleteEmergencyContactCommandHandler
{
    private readonly IEmergencyContactRepository _emergencyContactRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEmergencyContactCommandHandler(
        IEmergencyContactRepository emergencyContactRepository,
        IUnitOfWork unitOfWork)
    {
        _emergencyContactRepository = emergencyContactRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteEmergencyContactCommandResult> HandleAsync(
        DeleteEmergencyContactCommand command,
        CancellationToken cancellationToken = default)
    {
        var emergencyContact = await _emergencyContactRepository.GetByIdAsync(
            command.EmergencyContactId,
            cancellationToken);

        if (emergencyContact == null)
        {
            return new DeleteEmergencyContactCommandResult(false, "Emergency contact not found");
        }

        _emergencyContactRepository.Remove(emergencyContact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteEmergencyContactCommandResult(true);
    }
}

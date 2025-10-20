using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for UpdateWorkAuthorizationCommand
/// </summary>
public class UpdateWorkAuthorizationCommandHandler
{
    private readonly IWorkAuthorizationRepository _workAuthorizationRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWorkAuthorizationCommandHandler(
        IWorkAuthorizationRepository workAuthorizationRepository,
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork)
    {
        _workAuthorizationRepository = workAuthorizationRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Handles the command to update work authorization
    /// </summary>
    public async Task<bool> HandleAsync(
        UpdateWorkAuthorizationCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get existing authorization
        var authorization = await _workAuthorizationRepository.GetByIdAsync(
            command.AuthorizationId,
            cancellationToken);

        if (authorization == null)
        {
            return false;
        }

        // Validate document if provided
        if (command.RightToWorkDocumentId.HasValue)
        {
            var document = await _documentRepository.GetByIdAsync(
                command.RightToWorkDocumentId.Value,
                cancellationToken);

            if (document == null)
            {
                throw new InvalidOperationException(
                    $"Right-to-work document {command.RightToWorkDocumentId.Value} not found");
            }

            // Verify document belongs to the employee
            if (document.EmployeeId != authorization.EmployeeId)
            {
                throw new InvalidOperationException(
                    $"Document {command.RightToWorkDocumentId.Value} does not belong to employee {authorization.EmployeeId}");
            }
        }

        // Update authorization
        authorization.AuthorizationType = command.AuthorizationType;
        authorization.DocumentNumber = command.DocumentNumber;
        authorization.IssueDate = command.IssueDate;
        authorization.ExpirationDate = command.ExpirationDate;
        authorization.IssuingAuthority = command.IssuingAuthority;
        authorization.SponsorshipStatus = command.SponsorshipStatus;
        authorization.RightToWorkDocumentId = command.RightToWorkDocumentId;
        authorization.Notes = command.Notes;

        _workAuthorizationRepository.Update(authorization);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

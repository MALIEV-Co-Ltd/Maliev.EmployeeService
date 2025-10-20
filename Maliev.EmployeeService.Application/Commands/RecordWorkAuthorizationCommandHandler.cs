using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for RecordWorkAuthorizationCommand
/// </summary>
public class RecordWorkAuthorizationCommandHandler
{
    private readonly IWorkAuthorizationRepository _workAuthorizationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecordWorkAuthorizationCommandHandler> _logger;

    public RecordWorkAuthorizationCommandHandler(
        IWorkAuthorizationRepository workAuthorizationRepository,
        IEmployeeRepository employeeRepository,
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        ILogger<RecordWorkAuthorizationCommandHandler> logger)
    {
        _workAuthorizationRepository = workAuthorizationRepository;
        _employeeRepository = employeeRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the command to record work authorization
    /// </summary>
    public async Task<Guid> HandleAsync(
        RecordWorkAuthorizationCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee {command.EmployeeId} not found");
        }

        // Validate document exists if provided
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
            if (document.EmployeeId != command.EmployeeId)
            {
                throw new InvalidOperationException(
                    "Right-to-work document does not belong to the specified employee");
            }
        }

        // Create work authorization entity
        var workAuthorization = new WorkAuthorization
        {
            Id = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            AuthorizationType = command.AuthorizationType,
            DocumentNumber = command.DocumentNumber,
            IssueDate = command.IssueDate,
            ExpirationDate = command.ExpirationDate,
            IssuingAuthority = command.IssuingAuthority,
            SponsorshipStatus = command.SponsorshipStatus,
            RightToWorkDocumentId = command.RightToWorkDocumentId,
            Notes = command.Notes,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        await _workAuthorizationRepository.AddAsync(workAuthorization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded {AuthorizationType} authorization for employee {EmployeeId}, expires {ExpirationDate}",
            command.AuthorizationType,
            command.EmployeeId,
            command.ExpirationDate?.ToString("yyyy-MM-dd") ?? "never");

        return workAuthorization.Id;
    }
}

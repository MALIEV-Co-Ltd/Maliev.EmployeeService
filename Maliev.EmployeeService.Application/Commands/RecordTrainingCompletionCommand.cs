using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to record training completion
/// </summary>
public class RecordTrainingCompletionCommand
{
    public Guid EmployeeId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime CompletionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? CertificateDocumentId { get; set; }
    public TrainingType TrainingType { get; set; }
    public string? Provider { get; set; }
}

/// <summary>
/// Handler for RecordTrainingCompletionCommand
/// </summary>
public class RecordTrainingCompletionCommandHandler
{
    private readonly ITrainingRepository _trainingRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<RecordTrainingCompletionCommandHandler> _logger;

    public RecordTrainingCompletionCommandHandler(
        ITrainingRepository trainingRepository,
        IEmployeeRepository employeeRepository,
        ILogger<RecordTrainingCompletionCommandHandler> logger)
    {
        _trainingRepository = trainingRepository;
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    public async Task<TrainingRecord> HandleAsync(RecordTrainingCompletionCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording training completion for employee {EmployeeId}, course {CourseName}",
            command.EmployeeId, command.CourseName);

        // Verify employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee {command.EmployeeId} not found");
        }

        var trainingRecord = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            CourseName = command.CourseName,
            CompletionDate = command.CompletionDate,
            ExpirationDate = command.ExpirationDate,
            CertificateDocumentId = command.CertificateDocumentId,
            TrainingType = command.TrainingType,
            Provider = command.Provider,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Status will be set automatically by UpdateStatus() in repository
        var result = await _trainingRepository.CreateAsync(trainingRecord, cancellationToken);

        _logger.LogInformation("Training completion recorded for employee {EmployeeId}, training ID {TrainingId}, status {Status}",
            command.EmployeeId, result.Id, result.Status);

        return result;
    }
}

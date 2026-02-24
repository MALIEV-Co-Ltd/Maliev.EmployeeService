using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for creating a training record
/// </summary>
public class CreateTrainingRecordDto
{
    public string CourseName { get; set; } = string.Empty;
    public DateTime CompletionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? CertificateDocumentId { get; set; }
    public TrainingType TrainingType { get; set; }
    public string? Provider { get; set; }
}

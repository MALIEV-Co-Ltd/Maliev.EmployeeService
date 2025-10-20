using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Training record tracking completion and certification
/// </summary>
public class TrainingRecord : Entity
{
    /// <summary>
    /// Employee who completed the training
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Navigation property to Employee
    /// </summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Name of the training course
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Date when training was completed
    /// </summary>
    public DateTime CompletionDate { get; set; }

    /// <summary>
    /// Date when certification expires (null if no expiration)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Reference to certificate document in document storage system
    /// </summary>
    public string? CertificateDocumentId { get; set; }

    /// <summary>
    /// Type of training (Mandatory or Voluntary)
    /// </summary>
    public TrainingType TrainingType { get; set; }

    /// <summary>
    /// Training provider name
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Current certification status
    /// </summary>
    public CertificationStatus Status { get; set; }

    /// <summary>
    /// Determines the certification status based on expiration date
    /// </summary>
    public void UpdateStatus()
    {
        if (ExpirationDate == null)
        {
            Status = CertificationStatus.Valid;
            return;
        }

        var now = DateTime.UtcNow;
        var daysUntilExpiration = (ExpirationDate.Value - now).TotalDays;

        if (daysUntilExpiration < 0)
        {
            Status = CertificationStatus.Expired;
        }
        else if (daysUntilExpiration <= 60)
        {
            Status = CertificationStatus.Expiring;
        }
        else
        {
            Status = CertificationStatus.Valid;
        }
    }
}

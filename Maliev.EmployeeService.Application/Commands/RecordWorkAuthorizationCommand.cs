using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to record work authorization details for an employee
/// </summary>
public class RecordWorkAuthorizationCommand
{
    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Type of work authorization
    /// </summary>
    public AuthorizationType AuthorizationType { get; set; }

    /// <summary>
    /// Document/permit number
    /// </summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Issue date
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Expiration date (null for citizenship which doesn't expire)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Issuing authority
    /// </summary>
    public string IssuingAuthority { get; set; } = string.Empty;

    /// <summary>
    /// Sponsorship status
    /// </summary>
    public SponsorshipStatus SponsorshipStatus { get; set; }

    /// <summary>
    /// Reference to right-to-work document ID (optional)
    /// </summary>
    public Guid? RightToWorkDocumentId { get; set; }

    /// <summary>
    /// Notes/additional information
    /// </summary>
    public string? Notes { get; set; }
}

using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Work authorization details for employees (work permits, visas, citizenship)
/// Tracks expiration dates and sponsorship status for compliance
/// </summary>
public class WorkAuthorization : Entity
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
    /// Issuing authority (e.g., "Thailand Department of Employment", "US Immigration")
    /// </summary>
    public string IssuingAuthority { get; set; } = string.Empty;

    /// <summary>
    /// Sponsorship status
    /// </summary>
    public SponsorshipStatus SponsorshipStatus { get; set; }

    /// <summary>
    /// Reference to right-to-work document in Document entity (optional)
    /// </summary>
    public Guid? RightToWorkDocumentId { get; set; }

    /// <summary>
    /// Notes/additional information
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this authorization is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Employee this authorization belongs to
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Associated right-to-work document
    /// </summary>
    public Document? RightToWorkDocument { get; set; }
}

using Maliev.EmployeeService.Domain.Attributes;
using Maliev.EmployeeService.Domain.Common;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Document entity representing employee-related files stored securely
/// Supports encryption, version control, and role-based access control
/// </summary>
public class Document : Entity
{
    /// <summary>
    /// Employee this document belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Type of document (contract, ID, certificate, etc.)
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Storage path in Google Cloud Storage
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the document was uploaded
    /// </summary>
    public DateTime UploadDate { get; set; }

    /// <summary>
    /// Employee ID of the user who uploaded the document
    /// </summary>
    public Guid UploadedBy { get; set; }

    /// <summary>
    /// Current version number of the document
    /// </summary>
    public int VersionNumber { get; set; } = 1;

    /// <summary>
    /// Expiration date for documents that expire (e.g., work permits, certifications)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Access level determining who can view this document
    /// </summary>
    public AccessLevel AccessLevel { get; set; }

    /// <summary>
    /// Optional description or notes about the document
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// MIME type of the document (e.g., application/pdf, image/jpeg)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this document is archived
    /// </summary>
    public bool IsArchived { get; set; } = false;

    // Navigation Properties

    /// <summary>
    /// Employee this document belongs to
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// User who uploaded this document
    /// </summary>
    public Employee? UploadedByEmployee { get; set; }

    /// <summary>
    /// Version history for this document
    /// </summary>
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

    // Business Logic

    /// <summary>
    /// Checks if the document is expiring within the specified number of days
    /// </summary>
    public bool IsExpiringWithin(int days)
    {
        if (!ExpirationDate.HasValue)
            return false;

        var daysUntilExpiration = (ExpirationDate.Value - DateTime.UtcNow).TotalDays;
        return daysUntilExpiration <= days && daysUntilExpiration >= 0;
    }

    /// <summary>
    /// Checks if the document has expired
    /// </summary>
    public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;

    /// <summary>
    /// Gets days until expiration (null if no expiration date or already expired)
    /// </summary>
    public int? DaysUntilExpiration
    {
        get
        {
            if (!ExpirationDate.HasValue || IsExpired)
                return null;

            return (int)(ExpirationDate.Value - DateTime.UtcNow).TotalDays;
        }
    }
}

using Maliev.EmployeeService.Domain.Attributes;
using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Document version entity for tracking document history
/// Each new upload creates a new version while preserving previous ones
/// </summary>
public class DocumentVersion : Entity
{
    /// <summary>
    /// Reference to the parent document
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Version number of this document version
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Storage path in Google Cloud Storage for this version
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Original file name for this version
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when this version was uploaded
    /// </summary>
    public DateTime UploadDate { get; set; }

    /// <summary>
    /// Employee ID of the user who uploaded this version
    /// </summary>
    public Guid UploadedBy { get; set; }

    /// <summary>
    /// Description of changes in this version
    /// </summary>
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// File size in bytes for this version
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// MIME type of this document version
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    // Navigation Properties

    /// <summary>
    /// Parent document
    /// </summary>
    public Document? Document { get; set; }

    /// <summary>
    /// User who uploaded this version
    /// </summary>
    public Employee? UploadedByEmployee { get; set; }
}

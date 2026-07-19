using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for uploading a new document
/// </summary>
public class UploadDocumentDto
{
    /// <summary>
    /// Employee ID this document belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Type of document being uploaded
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Access level for this document
    /// </summary>
    public AccessLevel AccessLevel { get; set; }

    /// <summary>
    /// Optional description of the document
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional expiration date for time-sensitive documents
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>
/// Result of document upload operation
/// </summary>
public class UploadDocumentResultDto
{
    /// <summary>
    /// Document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Document type
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Encrypted file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Upload date
    /// </summary>
    public DateTime UploadDate { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Version number
    /// </summary>
    public int VersionNumber { get; set; }
}

using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for document information
/// </summary>
public class DocumentDto
{
    /// <summary>
    /// Document ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Document type
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// File name (decrypted)
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Upload date
    /// </summary>
    public DateTime UploadDate { get; set; }

    /// <summary>
    /// Uploaded by user ID
    /// </summary>
    public Guid UploadedBy { get; set; }

    /// <summary>
    /// Uploaded by user name
    /// </summary>
    public string UploadedByName { get; set; } = string.Empty;

    /// <summary>
    /// Current version number
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Expiration date (if applicable)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Access level
    /// </summary>
    public AccessLevel AccessLevel { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Is archived
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Is expired
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Days until expiration (null if no expiration or already expired)
    /// </summary>
    public int? DaysUntilExpiration { get; set; }
}

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for document version information
/// </summary>
public class DocumentVersionDto
{
    /// <summary>
    /// Version ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Version number
    /// </summary>
    public int VersionNumber { get; set; }

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
    /// Description of changes in this version
    /// </summary>
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}

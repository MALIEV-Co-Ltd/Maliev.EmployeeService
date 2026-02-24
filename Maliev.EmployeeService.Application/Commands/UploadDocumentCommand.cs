using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to upload a new document for an employee
/// </summary>
public class UploadDocumentCommand
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
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File content stream
    /// </summary>
    public Stream FileStream { get; set; } = Stream.Null;

    /// <summary>
    /// Content type (MIME type)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the document
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional expiration date for time-sensitive documents
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// ID of the user uploading the document
    /// </summary>
    public Guid UploadedBy { get; set; }
}

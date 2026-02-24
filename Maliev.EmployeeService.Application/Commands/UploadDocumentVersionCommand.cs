namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to upload a new version of an existing document
/// </summary>
public class UploadDocumentVersionCommand
{
    /// <summary>
    /// Document ID to create a new version for
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// New file name
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
    /// Description of changes in this version
    /// </summary>
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// ID of the user uploading the new version
    /// </summary>
    public Guid UploadedBy { get; set; }
}

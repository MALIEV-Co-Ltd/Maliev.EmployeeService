namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to download a document
/// </summary>
public class DownloadDocumentQuery
{
    /// <summary>
    /// Document ID to download
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Optional: Download a specific version (null = current version)
    /// </summary>
    public int? VersionNumber { get; set; }
}

/// <summary>
/// Result of document download
/// </summary>
public class DownloadDocumentResult
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File content stream
    /// </summary>
    public Stream FileStream { get; set; } = Stream.Null;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
}

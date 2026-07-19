namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Client for interacting with the Upload Service API (/uploads/v1)
/// Handles document upload, download, and deletion
/// </summary>
public interface IUploadServiceClient
{
    /// <summary>
    /// Uploads a document to the Upload Service
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <param name="fileStream">File content stream</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result containing storage path and file size</returns>
    Task<UploadResult> UploadAsync(
        string fileName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a document from the Upload Service
    /// </summary>
    /// <param name="storagePath">Storage path of the document</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the document content</returns>
    Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from the Upload Service
    /// </summary>
    /// <param name="storagePath">Storage path of the document to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document exists in the Upload Service
    /// </summary>
    /// <param name="storagePath">Storage path to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the document exists, false otherwise</returns>
    Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a file upload operation
/// </summary>
public class UploadResult
{
    /// <summary>
    /// Storage path where the file was uploaded
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Size of the uploaded file in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Content type of the uploaded file
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}

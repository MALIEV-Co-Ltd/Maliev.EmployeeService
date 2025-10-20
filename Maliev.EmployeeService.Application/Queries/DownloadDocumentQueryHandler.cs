using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for downloading a document
/// </summary>
public class DownloadDocumentQueryHandler
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUploadServiceClient _uploadServiceClient;
    private readonly ILogger<DownloadDocumentQueryHandler> _logger;

    public DownloadDocumentQueryHandler(
        IDocumentRepository documentRepository,
        IUploadServiceClient uploadServiceClient,
        ILogger<DownloadDocumentQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _uploadServiceClient = uploadServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Handles the download document query
    /// </summary>
    public async Task<DownloadDocumentResult> HandleAsync(
        DownloadDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading document {DocumentId}, version: {Version}",
            query.DocumentId, query.VersionNumber?.ToString() ?? "current");

        string storagePath;
        string fileName;
        string contentType;
        long fileSizeBytes;

        if (query.VersionNumber.HasValue)
        {
            // Download specific version
            var version = await _documentRepository.GetVersionAsync(
                query.DocumentId,
                query.VersionNumber.Value,
                cancellationToken);

            if (version == null)
            {
                throw new InvalidOperationException(
                    $"Document version {query.VersionNumber} not found for document {query.DocumentId}");
            }

            storagePath = version.StoragePath; // Already decrypted by EncryptionInterceptor
            fileName = version.FileName; // Already decrypted by EncryptionInterceptor
            contentType = version.ContentType;
            fileSizeBytes = version.FileSizeBytes;
        }
        else
        {
            // Download current version
            var document = await _documentRepository.GetByIdAsync(query.DocumentId, cancellationToken);

            if (document == null)
            {
                throw new InvalidOperationException($"Document {query.DocumentId} not found");
            }

            storagePath = document.StoragePath; // Already decrypted by EncryptionInterceptor
            fileName = document.FileName; // Already decrypted by EncryptionInterceptor
            contentType = document.ContentType;
            fileSizeBytes = document.FileSizeBytes;
        }

        // Download from Upload Service
        Stream fileStream;
        try
        {
            fileStream = await _uploadServiceClient.DownloadAsync(storagePath, cancellationToken);
            _logger.LogInformation("Document downloaded successfully from Upload Service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download document from Upload Service");
            throw new InvalidOperationException("Failed to download document from storage service", ex);
        }

        return new DownloadDocumentResult
        {
            FileName = fileName,
            ContentType = contentType,
            FileStream = fileStream,
            FileSizeBytes = fileSizeBytes
        };
    }
}

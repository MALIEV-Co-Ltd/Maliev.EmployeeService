using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for downloading a document
/// </summary>
public class DownloadDocumentQueryHandler
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUploadServiceClient _uploadServiceClient;
    private readonly IDocumentAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DownloadDocumentQueryHandler> _logger;

    public DownloadDocumentQueryHandler(
        IDocumentRepository documentRepository,
        IUploadServiceClient uploadServiceClient,
        IDocumentAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ILogger<DownloadDocumentQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _uploadServiceClient = uploadServiceClient;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _iamClient = iamClient;
        _configuration = configuration;
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

        var principalId = _currentUserService.PrincipalId;
        if (!principalId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Authorization check: User must have DocumentsRead permission
        // Path should be employee/{employeeId}/documents/{documentId}
        // But we need the document first to get the employeeId
        var document = await _documentRepository.GetByIdAsync(query.DocumentId, cancellationToken);
        if (document == null)
        {
            throw new InvalidOperationException($"Document {query.DocumentId} not found");
        }

        var resourcePath = $"employee/{document.EmployeeId}/documents/{document.Id}";
        if (!await _iamClient.CheckPermissionAsync(principalId.Value.ToString(), EmployeePermissions.DocumentsRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to download this document");
        }

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

            await _authorizationService.ValidateCanViewDocumentAsync(
                principalId.Value,
                document,
                cancellationToken);

            storagePath = version.StoragePath; // Already decrypted by EncryptionInterceptor
            fileName = version.FileName; // Already decrypted by EncryptionInterceptor
            contentType = version.ContentType;
            fileSizeBytes = version.FileSizeBytes;
        }
        else
        {
            // Authorization check
            await _authorizationService.ValidateCanViewDocumentAsync(
                principalId.Value,
                document,
                cancellationToken);

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

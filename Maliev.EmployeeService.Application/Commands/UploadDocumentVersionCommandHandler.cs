using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for uploading a new version of an existing document
/// </summary>
public class UploadDocumentVersionCommandHandler
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUploadServiceClient _uploadServiceClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadDocumentVersionCommandHandler> _logger;

    public UploadDocumentVersionCommandHandler(
        IDocumentRepository documentRepository,
        IEmployeeRepository employeeRepository,
        IUploadServiceClient uploadServiceClient,
        IUnitOfWork unitOfWork,
        ILogger<UploadDocumentVersionCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _employeeRepository = employeeRepository;
        _uploadServiceClient = uploadServiceClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the document version upload command
    /// </summary>
    public async Task<UploadDocumentResultDto> HandleAsync(
        UploadDocumentVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading new version for document {DocumentId}", command.DocumentId);

        // Get existing document with Versions collection for proper EF Core tracking
        var document = await _documentRepository.GetWithVersionsAsync(command.DocumentId, cancellationToken);
        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {command.DocumentId} not found");
        }

        // Validate uploader exists
        var uploader = await _employeeRepository.GetByIdAsync(command.UploadedBy, cancellationToken);
        if (uploader == null)
        {
            throw new InvalidOperationException($"Uploader with ID {command.UploadedBy} not found");
        }

        // Upload new file to Upload Service
        UploadResult uploadResult;
        try
        {
            uploadResult = await _uploadServiceClient.UploadAsync(
                command.FileName,
                command.FileStream,
                command.ContentType,
                cancellationToken);

            _logger.LogInformation("New version uploaded to Upload Service: {StoragePath}", uploadResult.StoragePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload new version to Upload Service");
            throw new InvalidOperationException("Failed to upload file to storage service", ex);
        }

        // Create version history entry for the CURRENT version before updating
        var currentVersion = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            VersionNumber = document.VersionNumber,
            StoragePath = document.StoragePath, // Current storage path
            FileName = document.FileName, // Current filename
            UploadDate = document.UploadDate,
            UploadedBy = document.UploadedBy,
            ChangeDescription = "Previous version archived",
            FileSizeBytes = document.FileSizeBytes,
            ContentType = document.ContentType
        };

        // Add version history directly to context (ensures proper EntityState.Added tracking)
        await _documentRepository.AddVersionAsync(currentVersion, cancellationToken);

        // Update document with new version information
        document.FileName = command.FileName;
        document.StoragePath = uploadResult.StoragePath;
        document.UploadDate = DateTime.UtcNow;
        document.UploadedBy = command.UploadedBy;
        document.VersionNumber += 1;
        document.FileSizeBytes = uploadResult.FileSizeBytes;
        document.ContentType = uploadResult.ContentType;

        // Save changes - entity is already tracked from GetWithVersionsAsync
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} updated to version {VersionNumber}",
            document.Id, document.VersionNumber);

        return new UploadDocumentResultDto
        {
            DocumentId = document.Id,
            EmployeeId = document.EmployeeId,
            DocumentType = document.DocumentType,
            FileName = command.FileName, // Return original filename, not encrypted
            UploadDate = document.UploadDate,
            FileSizeBytes = document.FileSizeBytes,
            VersionNumber = document.VersionNumber
        };
    }
}

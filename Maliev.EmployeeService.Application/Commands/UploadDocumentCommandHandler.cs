using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Handler for uploading a new document
/// </summary>
public class UploadDocumentCommandHandler
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUploadServiceClient _uploadServiceClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadDocumentCommandHandler> _logger;

    public UploadDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IEmployeeRepository employeeRepository,
        IUploadServiceClient uploadServiceClient,
        IUnitOfWork unitOfWork,
        ILogger<UploadDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _employeeRepository = employeeRepository;
        _uploadServiceClient = uploadServiceClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the document upload command
    /// </summary>
    public async Task<UploadDocumentResultDto> HandleAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading document for employee {EmployeeId}, type: {DocumentType}",
            command.EmployeeId, command.DocumentType);

        // Validate employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {command.EmployeeId} not found");
        }

        // Validate uploader exists
        var uploader = await _employeeRepository.GetByIdAsync(command.UploadedBy, cancellationToken);
        if (uploader == null)
        {
            throw new InvalidOperationException($"Uploader with ID {command.UploadedBy} not found");
        }

        // Upload file to Upload Service
        UploadResult uploadResult;
        try
        {
            uploadResult = await _uploadServiceClient.UploadAsync(
                command.FileName,
                command.FileStream,
                command.ContentType,
                cancellationToken);

            _logger.LogInformation("File uploaded to Upload Service: {StoragePath}", uploadResult.StoragePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Upload Service");
            throw new InvalidOperationException("Failed to upload file to storage service", ex);
        }

        // Create document entity (FileName and StoragePath will be encrypted by interceptor)
        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            DocumentType = command.DocumentType,
            FileName = command.FileName, // Will be encrypted by EncryptionInterceptor
            StoragePath = uploadResult.StoragePath, // Will be encrypted by EncryptionInterceptor
            UploadDate = DateTime.UtcNow,
            UploadedBy = command.UploadedBy,
            VersionNumber = 1,
            ExpirationDate = command.ExpirationDate,
            AccessLevel = command.AccessLevel,
            Description = command.Description,
            FileSizeBytes = uploadResult.FileSizeBytes,
            ContentType = uploadResult.ContentType,
            IsArchived = false
        };

        await _documentRepository.AddAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} created successfully for employee {EmployeeId}",
            document.Id, command.EmployeeId);

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

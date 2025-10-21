using Asp.Versioning;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Document management endpoints for secure document storage and retrieval
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly UploadDocumentCommandHandler _uploadDocumentHandler;
    private readonly UploadDocumentVersionCommandHandler _uploadVersionHandler;
    private readonly GetEmployeeDocumentsQueryHandler _getDocumentsHandler;
    private readonly DownloadDocumentQueryHandler _downloadDocumentHandler;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        UploadDocumentCommandHandler uploadDocumentHandler,
        UploadDocumentVersionCommandHandler uploadVersionHandler,
        GetEmployeeDocumentsQueryHandler getDocumentsHandler,
        DownloadDocumentQueryHandler downloadDocumentHandler,
        IDocumentRepository documentRepository,
        IDocumentAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ILogger<DocumentsController> logger)
    {
        _uploadDocumentHandler = uploadDocumentHandler;
        _uploadVersionHandler = uploadVersionHandler;
        _getDocumentsHandler = getDocumentsHandler;
        _downloadDocumentHandler = downloadDocumentHandler;
        _documentRepository = documentRepository;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all documents for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="documentType">Optional document type filter</param>
    /// <param name="includeArchived">Include archived documents</param>
    [HttpGet("employees/{employeeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetEmployeeDocuments(
        Guid employeeId,
        [FromQuery] DocumentType? documentType = null,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmployeeDocumentsQuery
        {
            EmployeeId = employeeId,
            DocumentType = documentType,
            IncludeArchived = includeArchived
        };

        var documents = await _getDocumentsHandler.HandleAsync(query, cancellationToken);

        // Filter documents based on user's access permissions
        var currentUserId = _currentUserService.EmployeeId ?? Guid.Empty;
        var currentUserRole = _currentUserService.PrimaryRole;

        var accessibleDocuments = new List<DocumentDto>();
        foreach (var doc in documents)
        {
            var document = await _documentRepository.GetByIdAsync(doc.Id, cancellationToken);
            if (document != null)
            {
                var canView = await _authorizationService.CanViewDocumentAsync(
                    currentUserId,
                    document,
                    currentUserRole,
                    cancellationToken);

                if (canView)
                {
                    accessibleDocuments.Add(doc);
                }
            }
        }

        return Ok(accessibleDocuments);
    }

    /// <summary>
    /// Upload a new document for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="dto">Document upload details</param>
    /// <param name="file">File to upload</param>
    [HttpPost("employees/{employeeId:guid}")]
    [ProducesResponseType(typeof(UploadDocumentResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadDocumentResultDto>> UploadDocument(
        Guid employeeId,
        [FromForm] UploadDocumentDto dto,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        // Validate file size (max 50MB)
        const long maxFileSize = 50 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest($"File size exceeds maximum allowed size of {maxFileSize / 1024 / 1024}MB");
        }

        var currentUserId = _currentUserService.EmployeeId ?? Guid.Empty;
        var currentUserRole = _currentUserService.PrimaryRole;

        // Check authorization
        await _authorizationService.ValidateCanUploadDocumentAsync(
            currentUserId,
            employeeId,
            currentUserRole,
            cancellationToken);

        // Create command
        var command = new UploadDocumentCommand
        {
            EmployeeId = employeeId,
            DocumentType = dto.DocumentType,
            AccessLevel = dto.AccessLevel,
            FileName = file.FileName,
            FileStream = file.OpenReadStream(),
            ContentType = file.ContentType,
            Description = dto.Description,
            ExpirationDate = dto.ExpirationDate,
            UploadedBy = currentUserId
        };

        var result = await _uploadDocumentHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(DownloadDocument),
            new { documentId = result.DocumentId },
            result);
    }

    /// <summary>
    /// Upload a new version of an existing document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="changeDescription">Description of changes</param>
    /// <param name="file">File to upload</param>
    [HttpPost("{documentId:guid}/versions")]
    [ProducesResponseType(typeof(UploadDocumentResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadDocumentResultDto>> UploadDocumentVersion(
        Guid documentId,
        [FromForm] string? changeDescription,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        // Validate file size (max 50MB)
        const long maxFileSize = 50 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest($"File size exceeds maximum allowed size of {maxFileSize / 1024 / 1024}MB");
        }

        // Get existing document
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return NotFound($"Document {documentId} not found");
        }

        var currentUserId = _currentUserService.EmployeeId ?? Guid.Empty;
        var currentUserRole = _currentUserService.PrimaryRole;

        // Check authorization (same as upload)
        await _authorizationService.ValidateCanUploadDocumentAsync(
            currentUserId,
            document.EmployeeId,
            currentUserRole,
            cancellationToken);

        // Create command
        var command = new UploadDocumentVersionCommand
        {
            DocumentId = documentId,
            FileName = file.FileName,
            FileStream = file.OpenReadStream(),
            ContentType = file.ContentType,
            ChangeDescription = changeDescription,
            UploadedBy = currentUserId
        };

        var result = await _uploadVersionHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(DownloadDocument),
            new { documentId = result.DocumentId },
            result);
    }

    /// <summary>
    /// Get all versions of a document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    [HttpGet("{documentId:guid}/versions")]
    [ProducesResponseType(typeof(IEnumerable<DocumentVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DocumentVersionDto>>> GetDocumentVersions(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        // Get document for authorization check
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return NotFound($"Document {documentId} not found");
        }

        var currentUserId = _currentUserService.EmployeeId ?? Guid.Empty;
        var currentUserRole = _currentUserService.PrimaryRole;

        // Check authorization
        await _authorizationService.ValidateCanViewDocumentAsync(
            currentUserId,
            document,
            currentUserRole,
            cancellationToken);

        // Get versions
        var versions = await _documentRepository.GetVersionsAsync(documentId, cancellationToken);

        var result = versions.Select(v => new DocumentVersionDto
        {
            Id = v.Id,
            DocumentId = v.DocumentId,
            VersionNumber = v.VersionNumber,
            FileName = v.FileName,
            UploadDate = v.UploadDate,
            UploadedBy = v.UploadedBy,
            ChangeDescription = v.ChangeDescription,
            FileSizeBytes = v.FileSizeBytes,
            ContentType = v.ContentType
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Download a document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="versionNumber">Optional version number (downloads current version if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{documentId:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(
        Guid documentId,
        [FromQuery] int? versionNumber = null,
        CancellationToken cancellationToken = default)
    {
        // Get document for authorization check
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return NotFound($"Document {documentId} not found");
        }

        var currentUserId = _currentUserService.EmployeeId ?? Guid.Empty;
        var currentUserRole = _currentUserService.PrimaryRole;

        // Check authorization
        await _authorizationService.ValidateCanViewDocumentAsync(
            currentUserId,
            document,
            currentUserRole,
            cancellationToken);

        // Download document
        var query = new DownloadDocumentQuery
        {
            DocumentId = documentId,
            VersionNumber = versionNumber
        };

        var result = await _downloadDocumentHandler.HandleAsync(query, cancellationToken);

        return File(result.FileStream, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Archive a document (soft delete)
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("{documentId:guid}")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveDocument(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return NotFound($"Document {documentId} not found");
        }

        var currentUserId = _currentUserService.EmployeeId ?? Guid.Empty;
        var currentUserRole = _currentUserService.PrimaryRole;

        // Check authorization
        await _authorizationService.ValidateCanDeleteDocumentAsync(
            currentUserId,
            document,
            currentUserRole,
            cancellationToken);

        await _documentRepository.ArchiveAsync(documentId, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Restore an archived document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{documentId:guid}/restore")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreDocument(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return NotFound($"Document {documentId} not found");
        }

        await _documentRepository.RestoreAsync(documentId, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Get expiring documents (HR only)
    /// </summary>
    /// <param name="daysUntilExpiration">Number of days until expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("expiring")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetExpiringDocuments(
        [FromQuery] int daysUntilExpiration = 90,
        CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetExpiringDocumentsAsync(daysUntilExpiration, cancellationToken);

        var result = documents.Select(d => new DocumentDto
        {
            Id = d.Id,
            EmployeeId = d.EmployeeId,
            DocumentType = d.DocumentType,
            FileName = d.FileName,
            UploadDate = d.UploadDate,
            UploadedBy = d.UploadedBy,
            UploadedByName = d.UploadedByEmployee?.LegalName?.FullName ?? "Unknown",
            VersionNumber = d.VersionNumber,
            ExpirationDate = d.ExpirationDate,
            AccessLevel = d.AccessLevel,
            Description = d.Description,
            FileSizeBytes = d.FileSizeBytes,
            ContentType = d.ContentType,
            IsArchived = d.IsArchived,
            IsExpired = d.ExpirationDate.HasValue && d.ExpirationDate.Value < DateTime.UtcNow,
            DaysUntilExpiration = d.ExpirationDate.HasValue && d.ExpirationDate.Value >= DateTime.UtcNow
                ? (int)(d.ExpirationDate.Value - DateTime.UtcNow).TotalDays
                : null
        }).ToList();

        return Ok(result);
    }
}

using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for getting employee documents
/// </summary>
public class GetEmployeeDocumentsQueryHandler
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<GetEmployeeDocumentsQueryHandler> _logger;

    public GetEmployeeDocumentsQueryHandler(
        IDocumentRepository documentRepository,
        ILogger<GetEmployeeDocumentsQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the get employee documents query
    /// </summary>
    public async Task<IEnumerable<DocumentDto>> HandleAsync(
        GetEmployeeDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting documents for employee {EmployeeId}, type: {DocumentType}",
            query.EmployeeId, query.DocumentType?.ToString() ?? "All");

        // Get documents based on filters
        var documents = query.DocumentType.HasValue
            ? await _documentRepository.GetByEmployeeIdAndTypeAsync(
                query.EmployeeId,
                query.DocumentType.Value,
                query.IncludeArchived,
                cancellationToken)
            : await _documentRepository.GetByEmployeeIdAsync(
                query.EmployeeId,
                query.IncludeArchived,
                cancellationToken);

        var now = DateTime.UtcNow;

        return documents.Select(d => new DocumentDto
        {
            Id = d.Id,
            EmployeeId = d.EmployeeId,
            DocumentType = d.DocumentType,
            FileName = d.FileName, // Already decrypted by EncryptionInterceptor
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
            IsExpired = d.ExpirationDate.HasValue && d.ExpirationDate.Value < now,
            DaysUntilExpiration = d.ExpirationDate.HasValue && d.ExpirationDate.Value >= now
                ? (int)(d.ExpirationDate.Value - now).TotalDays
                : null
        }).ToList();
    }
}

using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for getting employee documents
/// </summary>
public class GetEmployeeDocumentsQueryHandler
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetEmployeeDocumentsQueryHandler> _logger;

    public GetEmployeeDocumentsQueryHandler(
        IDocumentRepository documentRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ILogger<GetEmployeeDocumentsQueryHandler> logger)
    {
        _documentRepository = documentRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the get employee documents query
    /// </summary>
    public async Task<IEnumerable<DocumentDto>> HandleAsync(
        GetEmployeeDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have DocumentsView permission for this employee
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.EmployeeId}/documents";
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.DocumentsRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view documents for this employee");
        }

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

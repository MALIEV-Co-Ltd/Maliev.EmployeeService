using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Maliev.EmployeeService.Application.Services;

/// <summary>
/// Service for authorizing document access based on user roles and document access levels
/// </summary>
public class DocumentAuthorizationService : IDocumentAuthorizationService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly ILogger<DocumentAuthorizationService> _logger;
    private readonly IConfiguration _configuration;

    public DocumentAuthorizationService(
        IEmployeeRepository employeeRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ILogger<DocumentAuthorizationService> logger)
    {
        _employeeRepository = employeeRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> CanViewDocumentAsync(
        Guid principalId,
        Document document,
        CancellationToken cancellationToken = default)
    {
        var resourcePath = $"employee/{document.EmployeeId}/documents/{document.Id}";

        // Check if user has read permission on this specific document
        var hasPermission = await _iamClient.CheckPermissionAsync(
            principalId.ToString(), 
            EmployeePermissions.DocumentsRead, 
            resourcePath, 
            cancellationToken);

        if (hasPermission)
        {
            _logger.LogDebug("IAM granted access to document {DocumentId} for user {PrincipalId}", document.Id, principalId);
            return true;
        }

        // Public documents are always viewable if IAM doesn't explicitly deny (and we might have a policy for this)
        if (document.AccessLevel == AccessLevel.Public)
        {
            return true;
        }

        _logger.LogWarning("IAM denied access to document {DocumentId} for user {PrincipalId}", document.Id, principalId);
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> CanUploadDocumentAsync(
        Guid principalId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var resourcePath = $"employee/{employeeId}";

        return await _iamClient.CheckPermissionAsync(
            principalId.ToString(),
            EmployeePermissions.DocumentsCreate,
            resourcePath,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CanDeleteDocumentAsync(
        Guid principalId,
        Document document,
        CancellationToken cancellationToken = default)
    {
        var resourcePath = $"employee/{document.EmployeeId}/documents/{document.Id}";

        return await _iamClient.CheckPermissionAsync(
            principalId.ToString(),
            EmployeePermissions.DocumentsDelete,
            resourcePath,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ValidateCanViewDocumentAsync(
        Guid principalId,
        Document document,
        CancellationToken cancellationToken = default)
    {
        var canView = await CanViewDocumentAsync(principalId, document, cancellationToken);
        if (!canView)
        {
            throw new UnauthorizedAccessException(
                $"User {principalId} is not authorized to view document {document.Id}");
        }
    }

    /// <inheritdoc/>
    public async Task ValidateCanUploadDocumentAsync(
        Guid principalId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var canUpload = await CanUploadDocumentAsync(principalId, employeeId, cancellationToken);
        if (!canUpload)
        {
            throw new UnauthorizedAccessException(
                $"User {principalId} is not authorized to upload documents for employee {employeeId}");
        }
    }

    /// <inheritdoc/>
    public async Task ValidateCanDeleteDocumentAsync(
        Guid principalId,
        Document document,
        CancellationToken cancellationToken = default)
    {
        var canDelete = await CanDeleteDocumentAsync(principalId, document, cancellationToken);
        if (!canDelete)
        {
            throw new UnauthorizedAccessException(
                $"User {principalId} is not authorized to delete document {document.Id}");
        }
    }
}

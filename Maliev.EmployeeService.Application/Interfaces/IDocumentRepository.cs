using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository for Document entity operations
/// </summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>
    /// Gets all documents for a specific employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="includeArchived">Include archived documents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    Task<IEnumerable<Document>> GetByEmployeeIdAsync(
        Guid employeeId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents for a specific employee filtered by document type
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="documentType">Document type</param>
    /// <param name="includeArchived">Include archived documents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    Task<IEnumerable<Document>> GetByEmployeeIdAndTypeAsync(
        Guid employeeId,
        DocumentType documentType,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents expiring within the specified number of days
    /// </summary>
    /// <param name="daysUntilExpiration">Number of days until expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expiring documents</returns>
    Task<IEnumerable<Document>> GetExpiringDocumentsAsync(
        int daysUntilExpiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all expired documents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired documents</returns>
    Task<IEnumerable<Document>> GetExpiredDocumentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents uploaded by a specific user
    /// </summary>
    /// <param name="uploadedBy">Uploader user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents</returns>
    Task<IEnumerable<Document>> GetByUploaderAsync(
        Guid uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of a document with all its version history
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document with version history</returns>
    Task<Document?> GetWithVersionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version of a document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="versionNumber">Version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document version</returns>
    Task<DocumentVersion?> GetVersionAsync(
        Guid documentId,
        int versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a document ordered by version number descending
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document versions</returns>
    Task<IEnumerable<DocumentVersion>> GetVersionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a document (soft delete)
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ArchiveAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an archived document
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RestoreAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new document version to the context
    /// </summary>
    /// <param name="documentVersion">Document version to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddVersionAsync(
        DocumentVersion documentVersion,
        CancellationToken cancellationToken = default);
}

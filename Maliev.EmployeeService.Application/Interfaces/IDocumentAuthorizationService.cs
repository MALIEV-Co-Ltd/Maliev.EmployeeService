using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Service for authorizing document access based on user roles and document access levels
/// </summary>
public interface IDocumentAuthorizationService
{
    /// <summary>
    /// Checks if a user can view a document
    /// </summary>
    /// <param name="principalId">The principal identity (IAM) of the user</param>
    /// <param name="document">Document to check access for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can view the document</returns>
    Task<bool> CanViewDocumentAsync(
        Guid principalId,
        Document document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can upload a document for an employee
    /// </summary>
    /// <param name="principalId">The principal identity (IAM) of the user</param>
    /// <param name="employeeId">Employee ID the document is for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can upload the document</returns>
    Task<bool> CanUploadDocumentAsync(
        Guid principalId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can delete a document
    /// </summary>
    /// <param name="principalId">The principal identity (IAM) of the user</param>
    /// <param name="document">Document to check access for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can delete the document</returns>
    Task<bool> CanDeleteDocumentAsync(
        Guid principalId,
        Document document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and throws exception if user cannot view document
    /// </summary>
    Task ValidateCanViewDocumentAsync(
        Guid principalId,
        Document document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and throws exception if user cannot upload document
    /// </summary>
    Task ValidateCanUploadDocumentAsync(
        Guid principalId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and throws exception if user cannot delete document
    /// </summary>
    Task ValidateCanDeleteDocumentAsync(
        Guid principalId,
        Document document,
        CancellationToken cancellationToken = default);
}

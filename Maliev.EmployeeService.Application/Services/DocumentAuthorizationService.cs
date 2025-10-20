using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Services;

/// <summary>
/// Service for authorizing document access based on user roles and document access levels
/// </summary>
public class DocumentAuthorizationService : IDocumentAuthorizationService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<DocumentAuthorizationService> _logger;

    public DocumentAuthorizationService(
        IEmployeeRepository employeeRepository,
        ILogger<DocumentAuthorizationService> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> CanViewDocumentAsync(
        Guid userId,
        Document document,
        Role userRole,
        CancellationToken cancellationToken = default)
    {
        // System administrators can view all documents
        if (userRole == Role.SystemAdministrator)
        {
            _logger.LogDebug("Admin user {UserId} granted access to document {DocumentId}", userId, document.Id);
            return true;
        }

        // Check access level
        switch (document.AccessLevel)
        {
            case AccessLevel.Public:
                // Anyone can view public documents
                return true;

            case AccessLevel.Employee:
                // Only the employee themselves can view
                if (document.EmployeeId == userId)
                {
                    return true;
                }
                break;

            case AccessLevel.Manager:
                // Employee and their manager can view
                if (document.EmployeeId == userId)
                {
                    return true;
                }

                // Check if user is the employee's manager
                var employee = await _employeeRepository.GetByIdAsync(document.EmployeeId, cancellationToken);
                if (employee?.ManagerId == userId)
                {
                    _logger.LogDebug("Manager {UserId} granted access to document {DocumentId} for employee {EmployeeId}",
                        userId, document.Id, document.EmployeeId);
                    return true;
                }
                break;

            case AccessLevel.HROnly:
                // Only HR personnel (HRGeneralist, HRSpecialist, or SystemAdministrator) can view
                if (userRole == Role.HRGeneralist || userRole == Role.HRSpecialist)
                {
                    return true;
                }
                break;

            case AccessLevel.HRSpecialistOnly:
                // Only HR specialists can view
                if (userRole == Role.HRSpecialist)
                {
                    return true;
                }
                break;
        }

        _logger.LogWarning("User {UserId} denied access to document {DocumentId} with access level {AccessLevel}",
            userId, document.Id, document.AccessLevel);
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> CanUploadDocumentAsync(
        Guid userId,
        Guid employeeId,
        Role userRole,
        CancellationToken cancellationToken = default)
    {
        // System Administrator and HR can upload for anyone
        if (userRole == Role.SystemAdministrator || userRole == Role.HRGeneralist || userRole == Role.HRSpecialist)
        {
            return true;
        }

        // Managers can upload for their direct reports
        if (userRole == Role.Manager)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee?.ManagerId == userId)
            {
                _logger.LogDebug("Manager {UserId} granted upload permission for employee {EmployeeId}",
                    userId, employeeId);
                return true;
            }
        }

        // Employees can upload their own documents
        if (employeeId == userId)
        {
            return true;
        }

        _logger.LogWarning("User {UserId} denied upload permission for employee {EmployeeId}",
            userId, employeeId);
        return false;
    }

    /// <inheritdoc/>
    public Task<bool> CanDeleteDocumentAsync(
        Guid userId,
        Document document,
        Role userRole,
        CancellationToken cancellationToken = default)
    {
        // Only SystemAdministrator and HR can delete documents
        var canDelete = userRole == Role.SystemAdministrator || userRole == Role.HRGeneralist || userRole == Role.HRSpecialist;

        if (!canDelete)
        {
            _logger.LogWarning("User {UserId} denied delete permission for document {DocumentId}",
                userId, document.Id);
        }

        return Task.FromResult(canDelete);
    }

    /// <inheritdoc/>
    public async Task ValidateCanViewDocumentAsync(
        Guid userId,
        Document document,
        Role userRole,
        CancellationToken cancellationToken = default)
    {
        var canView = await CanViewDocumentAsync(userId, document, userRole, cancellationToken);
        if (!canView)
        {
            throw new UnauthorizedAccessException(
                $"User {userId} is not authorized to view document {document.Id}");
        }
    }

    /// <inheritdoc/>
    public async Task ValidateCanUploadDocumentAsync(
        Guid userId,
        Guid employeeId,
        Role userRole,
        CancellationToken cancellationToken = default)
    {
        var canUpload = await CanUploadDocumentAsync(userId, employeeId, userRole, cancellationToken);
        if (!canUpload)
        {
            throw new UnauthorizedAccessException(
                $"User {userId} is not authorized to upload documents for employee {employeeId}");
        }
    }

    /// <inheritdoc/>
    public async Task ValidateCanDeleteDocumentAsync(
        Guid userId,
        Document document,
        Role userRole,
        CancellationToken cancellationToken = default)
    {
        var canDelete = await CanDeleteDocumentAsync(userId, document, userRole, cancellationToken);
        if (!canDelete)
        {
            throw new UnauthorizedAccessException(
                $"User {userId} is not authorized to delete document {document.Id}");
        }
    }
}

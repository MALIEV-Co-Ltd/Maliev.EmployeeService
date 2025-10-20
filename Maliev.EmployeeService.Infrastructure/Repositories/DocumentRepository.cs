using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Document entity
/// </summary>
public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(EmployeeServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetByEmployeeIdAsync(
        Guid employeeId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .Where(d => d.EmployeeId == employeeId);

        if (!includeArchived)
        {
            query = query.Where(d => !d.IsArchived);
        }

        return await query
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetByEmployeeIdAndTypeAsync(
        Guid employeeId,
        DocumentType documentType,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .Where(d => d.EmployeeId == employeeId && d.DocumentType == documentType);

        if (!includeArchived)
        {
            query = query.Where(d => !d.IsArchived);
        }

        return await query
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetExpiringDocumentsAsync(
        int daysUntilExpiration,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expirationThreshold = now.AddDays(daysUntilExpiration);

        return await _context.Documents
            .Where(d => !d.IsArchived &&
                       d.ExpirationDate.HasValue &&
                       d.ExpirationDate.Value >= now &&
                       d.ExpirationDate.Value <= expirationThreshold)
            .Include(d => d.Employee)
            .OrderBy(d => d.ExpirationDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetExpiredDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Documents
            .Where(d => !d.IsArchived &&
                       d.ExpirationDate.HasValue &&
                       d.ExpirationDate.Value < now)
            .Include(d => d.Employee)
            .OrderBy(d => d.ExpirationDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Document>> GetByUploaderAsync(
        Guid uploadedBy,
        CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.UploadedBy == uploadedBy)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Document?> GetWithVersionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        // Load document WITH its Versions collection so EF Core properly tracks new additions
        // Now that encryption is via value converters (not interceptor), Include works correctly
        return await _context.Documents
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DocumentVersion?> GetVersionAsync(
        Guid documentId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentVersions
            .Include(v => v.UploadedByEmployee)
            .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.VersionNumber == versionNumber,
                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DocumentVersion>> GetVersionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ArchiveAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document != null)
        {
            document.IsArchived = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document != null)
        {
            document.IsArchived = false;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task AddVersionAsync(
        DocumentVersion documentVersion,
        CancellationToken cancellationToken = default)
    {
        await _context.DocumentVersions.AddAsync(documentVersion, cancellationToken);
    }
}

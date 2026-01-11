using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for BulkJob entity
/// User Story 12 - Bulk Operations
/// </summary>
public class BulkJobRepository : Repository<BulkJob>, IBulkJobRepository
{
    public BulkJobRepository(EmployeeDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get a bulk job by its job ID
    /// </summary>
    public async Task<BulkJob?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BulkJob>()
            .FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);
    }

    /// <summary>
    /// Get pending jobs that need to be processed
    /// </summary>
    public async Task<List<BulkJob>> GetPendingJobsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BulkJob>()
            .Where(j => j.Status == BulkJobStatus.Pending)
            .OrderBy(j => j.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get jobs initiated by a specific user
    /// </summary>
    public async Task<List<BulkJob>> GetJobsByUserAsync(Guid principalId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BulkJob>()
            .Where(j => j.InitiatedByPrincipalId == principalId)
            .OrderByDescending(j => j.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}

using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for BulkJob entity
/// User Story 12 - Bulk Operations
/// </summary>
public interface IBulkJobRepository : IRepository<BulkJob>
{
    /// <summary>
    /// Get a bulk job by its job ID
    /// </summary>
    Task<BulkJob?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending jobs that need to be processed
    /// </summary>
    Task<List<BulkJob>> GetPendingJobsAsync(int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get jobs initiated by a specific user
    /// </summary>
    Task<List<BulkJob>> GetJobsByUserAsync(Guid principalId, int limit = 50, CancellationToken cancellationToken = default);
}

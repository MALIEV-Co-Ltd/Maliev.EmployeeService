using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for PerformanceReview entity
/// Supports performance review management operations
/// </summary>
public interface IPerformanceReviewRepository : IRepository<PerformanceReview>
{
    /// <summary>
    /// Get performance review with related goals
    /// </summary>
    Task<PerformanceReview?> GetWithGoalsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all performance reviews for an employee (as the subject being reviewed)
    /// </summary>
    Task<IEnumerable<PerformanceReview>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all performance reviews conducted by a reviewer (manager)
    /// </summary>
    Task<IEnumerable<PerformanceReview>> GetByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance reviews by review cycle type
    /// </summary>
    Task<IEnumerable<PerformanceReview>> GetByReviewCycleAsync(ReviewCycle reviewCycle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance reviews within a specific period range
    /// </summary>
    Task<IEnumerable<PerformanceReview>> GetByReviewPeriodAsync(DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance reviews pending employee acknowledgment
    /// </summary>
    Task<IEnumerable<PerformanceReview>> GetPendingAcknowledgmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance reviews by status (Draft, Submitted, Acknowledged)
    /// </summary>
    Task<IEnumerable<PerformanceReview>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance reviews for an employee within a date range
    /// </summary>
    Task<IEnumerable<PerformanceReview>> GetByEmployeeAndPeriodAsync(Guid employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the most recent performance review for an employee
    /// </summary>
    Task<PerformanceReview?> GetMostRecentForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

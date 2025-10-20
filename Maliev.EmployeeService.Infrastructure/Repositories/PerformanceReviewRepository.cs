using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for PerformanceReview entity
/// </summary>
public class PerformanceReviewRepository : Repository<PerformanceReview>, IPerformanceReviewRepository
{
    public PerformanceReviewRepository(EmployeeServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<PerformanceReview?> GetWithGoalsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(pr => pr.Employee)
            .Include(pr => pr.Reviewer)
            .Include(pr => pr.Goals)
            .AsSplitQuery() // Phase 16 - T388: Prevent cartesian explosion with Goals collection
            .FirstOrDefaultAsync(pr => pr.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PerformanceReview>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pr => pr.EmployeeId == employeeId)
            .Include(pr => pr.Reviewer)
            .OrderByDescending(pr => pr.ReviewPeriodEnd)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PerformanceReview>> GetByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pr => pr.ReviewerId == reviewerId)
            .Include(pr => pr.Employee)
            .OrderByDescending(pr => pr.ReviewPeriodEnd)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PerformanceReview>> GetByReviewCycleAsync(ReviewCycle reviewCycle, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pr => pr.ReviewCycle == reviewCycle)
            .Include(pr => pr.Employee)
            .Include(pr => pr.Reviewer)
            .OrderByDescending(pr => pr.ReviewPeriodEnd)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PerformanceReview>> GetByReviewPeriodAsync(DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pr => pr.ReviewPeriodStart >= periodStart && pr.ReviewPeriodEnd <= periodEnd)
            .Include(pr => pr.Employee)
            .Include(pr => pr.Reviewer)
            .OrderBy(pr => pr.ReviewPeriodStart)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PerformanceReview>> GetPendingAcknowledgmentAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pr => pr.Status == "Submitted" && pr.AcknowledgedDate == null)
            .Include(pr => pr.Employee)
            .Include(pr => pr.Reviewer)
            .OrderBy(pr => pr.ReviewDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PerformanceReview>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pr => pr.Status == status)
            .Include(pr => pr.Employee)
            .Include(pr => pr.Reviewer)
            .OrderByDescending(pr => pr.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PerformanceReview>> GetByEmployeeAndPeriodAsync(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pr => pr.EmployeeId == employeeId &&
                        pr.ReviewPeriodStart >= startDate &&
                        pr.ReviewPeriodEnd <= endDate)
            .Include(pr => pr.Reviewer)
            .OrderBy(pr => pr.ReviewPeriodStart)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PerformanceReview?> GetMostRecentForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(pr => pr.EmployeeId == employeeId)
            .Include(pr => pr.Reviewer)
            .Include(pr => pr.Goals)
            .AsSplitQuery() // Phase 16 - T388: Prevent cartesian explosion with Goals collection
            .OrderByDescending(pr => pr.ReviewPeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

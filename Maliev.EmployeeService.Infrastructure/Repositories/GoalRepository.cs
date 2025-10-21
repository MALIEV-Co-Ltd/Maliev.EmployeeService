using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Goal entity
/// </summary>
public class GoalRepository : Repository<Goal>, IGoalRepository
{
    public GoalRepository(EmployeeServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Goal>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.EmployeeId == employeeId)
            .Include(g => g.PerformanceReview)
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Goal>> GetByPerformanceReviewIdAsync(Guid performanceReviewId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.PerformanceReviewId == performanceReviewId)
            .Include(g => g.Employee)
            .OrderBy(g => g.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Goal>> GetByStatusAsync(GoalStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.CompletionStatus == status)
            .Include(g => g.Employee)
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Goal>> GetByTargetDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.TargetDate >= startDate && g.TargetDate <= endDate)
            .Include(g => g.Employee)
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Goal>> GetOverdueGoalsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbSet
            .Where(g => g.TargetDate < today &&
                       g.CompletionStatus != GoalStatus.Completed &&
                       g.CompletionStatus != GoalStatus.Cancelled)
            .Include(g => g.Employee)
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Goal>> GetOverdueGoalsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbSet
            .Where(g => g.EmployeeId == employeeId &&
                       g.TargetDate < today &&
                       g.CompletionStatus != GoalStatus.Completed &&
                       g.CompletionStatus != GoalStatus.Cancelled)
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Goal>> GetByEmployeeAndStatusAsync(Guid employeeId, GoalStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.EmployeeId == employeeId && g.CompletionStatus == status)
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Goal>> GetActiveGoalsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.EmployeeId == employeeId && g.CompletionStatus == GoalStatus.InProgress)
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Goal>> GetTeamGoalsAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.Employee!.ManagerId == managerId)
            .Include(g => g.Employee)
            .OrderBy(g => g.Employee!.LegalName.LastName)
            .ThenBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }
}

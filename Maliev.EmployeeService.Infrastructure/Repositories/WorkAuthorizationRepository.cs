using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for WorkAuthorization entity
/// </summary>
public class WorkAuthorizationRepository : Repository<WorkAuthorization>, IWorkAuthorizationRepository
{
    public WorkAuthorizationRepository(EmployeeDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WorkAuthorization>> GetByEmployeeIdAsync(
        Guid employeeId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkAuthorizations
            .Where(w => w.EmployeeId == employeeId);

        if (!includeInactive)
        {
            query = query.Where(w => w.IsActive);
        }

        return await query
            .OrderByDescending(w => w.IssueDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WorkAuthorization>> GetExpiringAsync(
        int daysUntilExpiration,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expirationThreshold = now.AddDays(daysUntilExpiration);

        return await _context.WorkAuthorizations
            .Where(w => w.IsActive &&
                       w.ExpirationDate.HasValue &&
                       w.ExpirationDate.Value >= now &&
                       w.ExpirationDate.Value <= expirationThreshold)
            .Include(w => w.Employee)
            .OrderBy(w => w.ExpirationDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WorkAuthorization>> GetExpiredAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.WorkAuthorizations
            .Where(w => w.IsActive &&
                       w.ExpirationDate.HasValue &&
                       w.ExpirationDate.Value < now)
            .Include(w => w.Employee)
            .OrderBy(w => w.ExpirationDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<WorkAuthorization?> GetActiveByEmployeeIdAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkAuthorizations
            .Where(w => w.EmployeeId == employeeId && w.IsActive)
            .OrderByDescending(w => w.IssueDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, int>> GetSponsorshipStatusSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkAuthorizations
            .Where(w => w.IsActive)
            .GroupBy(w => w.SponsorshipStatus)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }
}

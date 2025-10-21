using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for LeavePolicy entity
/// </summary>
public class LeavePolicyRepository : Repository<LeavePolicy>, ILeavePolicyRepository
{
    public LeavePolicyRepository(EmployeeServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<LeavePolicy?> GetByLeaveTypeAsync(LeaveType leaveType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lp => lp.LeaveType == leaveType && lp.IsActive)
            .OrderByDescending(lp => lp.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeavePolicy>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lp => lp.IsActive)
            .OrderBy(lp => lp.LeaveType)
            .ThenByDescending(lp => lp.EffectiveDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LeavePolicy?> GetByLeaveTypeAndDateAsync(
        LeaveType leaveType,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lp => lp.LeaveType == leaveType &&
                        lp.IsActive &&
                        lp.EffectiveDate <= effectiveDate)
            .OrderByDescending(lp => lp.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for LeaveBalance entity
/// </summary>
public class LeaveBalanceRepository : Repository<LeaveBalance>, ILeaveBalanceRepository
{
    public LeaveBalanceRepository(EmployeeDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveBalance>> GetByEmployeeAndYearAsync(Guid employeeId, int year, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == year)
            .OrderBy(lb => lb.LeaveType)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LeaveBalance?> GetByEmployeeLeaveTypeAndYearAsync(
        Guid employeeId,
        LeaveType leaveType,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId &&
                                      lb.LeaveType == leaveType &&
                                      lb.Year == year,
                                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveBalance>> GetCurrentYearBalancesAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var currentYear = DateTime.UtcNow.Year;
        return await GetByEmployeeAndYearAsync(employeeId, currentYear, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateUsedDaysAsync(Guid balanceId, decimal usedDays, CancellationToken cancellationToken = default)
    {
        var balance = await _dbSet.FindAsync(new object[] { balanceId }, cancellationToken);
        if (balance != null)
        {
            balance.UsedDays = usedDays;
        }
    }

    /// <inheritdoc/>
    public async Task UpdatePendingDaysAsync(Guid balanceId, decimal pendingDays, CancellationToken cancellationToken = default)
    {
        var balance = await _dbSet.FindAsync(new object[] { balanceId }, cancellationToken);
        if (balance != null)
        {
            balance.PendingDays = pendingDays;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveBalance>> GetExpiringBalancesAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(lb => lb.Employee)
            .Where(lb => lb.ExpiryDate.HasValue &&
                        lb.ExpiryDate.Value.Date <= beforeDate.Date &&
                        lb.AvailableDays > 0)
            .OrderBy(lb => lb.ExpiryDate)
            .ToListAsync(cancellationToken);
    }
}

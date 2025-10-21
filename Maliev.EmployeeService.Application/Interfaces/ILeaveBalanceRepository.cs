using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for LeaveBalance entity
/// </summary>
public interface ILeaveBalanceRepository : IRepository<LeaveBalance>
{
    /// <summary>
    /// Get all leave balances for an employee for a specific year
    /// </summary>
    Task<IEnumerable<LeaveBalance>> GetByEmployeeAndYearAsync(Guid employeeId, int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get specific leave balance for employee, leave type, and year
    /// </summary>
    Task<LeaveBalance?> GetByEmployeeLeaveTypeAndYearAsync(Guid employeeId, LeaveType leaveType, int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all current year balances for an employee
    /// </summary>
    Task<IEnumerable<LeaveBalance>> GetCurrentYearBalancesAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update used days for a leave balance
    /// </summary>
    Task UpdateUsedDaysAsync(Guid balanceId, decimal usedDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update pending days for a leave balance
    /// </summary>
    Task UpdatePendingDaysAsync(Guid balanceId, decimal pendingDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all leave balances expiring before the specified date
    /// </summary>
    Task<IEnumerable<LeaveBalance>> GetExpiringBalancesAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
}

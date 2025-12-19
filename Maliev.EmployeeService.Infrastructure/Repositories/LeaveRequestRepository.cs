using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for LeaveRequest entity
/// </summary>
public class LeaveRequestRepository : Repository<LeaveRequest>, ILeaveRequestRepository
{
    public LeaveRequestRepository(EmployeeDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<LeaveRequest?> GetWithApprovalsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(lr => lr.Employee)
            .Include(lr => lr.Approver)
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lr => lr.EmployeeId == employeeId)
            .OrderByDescending(lr => lr.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveRequest>> GetByStatusAsync(LeaveRequestStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lr => lr.Status == status)
            .Include(lr => lr.Employee)
            .OrderByDescending(lr => lr.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveRequest>> GetPendingForApproverAsync(Guid approverId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lr => lr.ApproverId == approverId &&
                        lr.Status == LeaveRequestStatus.Pending)
            .Include(lr => lr.Employee)
            .OrderBy(lr => lr.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveRequest>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lr => lr.StartDate <= endDate && lr.EndDate >= startDate)
            .Include(lr => lr.Employee)
            .OrderBy(lr => lr.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveRequest>> GetOverlappingRequestsAsync(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lr => lr.EmployeeId == employeeId &&
                        lr.StartDate <= endDate &&
                        lr.EndDate >= startDate &&
                        lr.Status != LeaveRequestStatus.Denied &&
                        lr.Status != LeaveRequestStatus.Cancelled)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveRequest>> GetTeamLeaveCalendarAsync(
        Guid managerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lr => lr.Employee!.ManagerId == managerId &&
                        lr.Status == LeaveRequestStatus.Approved &&
                        lr.StartDate <= endDate &&
                        lr.EndDate >= startDate)
            .Include(lr => lr.Employee)
            .OrderBy(lr => lr.StartDate)
            .ToListAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for LeaveApproval entity
/// </summary>
public class LeaveApprovalRepository : Repository<LeaveApproval>, ILeaveApprovalRepository
{
    public LeaveApprovalRepository(EmployeeDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveApproval>> GetByLeaveRequestIdAsync(Guid leaveRequestId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(la => la.LeaveRequestId == leaveRequestId)
            .Include(la => la.Approver)
            .OrderBy(la => la.ApprovalLevel)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LeaveApproval?> GetByLeaveRequestAndLevelAsync(
        Guid leaveRequestId,
        int approvalLevel,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(la => la.Approver)
            .FirstOrDefaultAsync(la => la.LeaveRequestId == leaveRequestId &&
                                      la.ApprovalLevel == approvalLevel,
                                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LeaveApproval>> GetByApproverIdAsync(Guid approverId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(la => la.ApproverId == approverId)
            .Include(la => la.LeaveRequest)
            .ThenInclude(lr => lr!.Employee)
            .OrderByDescending(la => la.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> AreAllApprovalsCompletedAsync(
        Guid leaveRequestId,
        int requiredApprovalLevels,
        CancellationToken cancellationToken = default)
    {
        var approvedCount = await _dbSet
            .CountAsync(la => la.LeaveRequestId == leaveRequestId &&
                             la.Decision == ApprovalDecision.Approved,
                       cancellationToken);

        return approvedCount >= requiredApprovalLevels;
    }
}

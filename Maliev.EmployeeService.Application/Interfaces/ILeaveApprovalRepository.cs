using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for LeaveApproval entity
/// </summary>
public interface ILeaveApprovalRepository : IRepository<LeaveApproval>
{
    /// <summary>
    /// Get all approvals for a leave request
    /// </summary>
    Task<IEnumerable<LeaveApproval>> GetByLeaveRequestIdAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval at specific level for a leave request
    /// </summary>
    Task<LeaveApproval?> GetByLeaveRequestAndLevelAsync(Guid leaveRequestId, int approvalLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all approvals made by a specific approver
    /// </summary>
    Task<IEnumerable<LeaveApproval>> GetByApproverIdAsync(Guid approverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if all required approvals are completed for a leave request
    /// </summary>
    Task<bool> AreAllApprovalsCompletedAsync(Guid leaveRequestId, int requiredApprovalLevels, CancellationToken cancellationToken = default);
}

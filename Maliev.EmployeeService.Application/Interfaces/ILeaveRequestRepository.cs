using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for LeaveRequest entity
/// </summary>
public interface ILeaveRequestRepository : IRepository<LeaveRequest>
{
    /// <summary>
    /// Get leave request with approvals
    /// </summary>
    Task<LeaveRequest?> GetWithApprovalsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all leave requests for an employee
    /// </summary>
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get leave requests by status
    /// </summary>
    Task<IEnumerable<LeaveRequest>> GetByStatusAsync(LeaveRequestStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending leave requests for approval by approver
    /// </summary>
    Task<IEnumerable<LeaveRequest>> GetPendingForApproverAsync(Guid approverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get leave requests within date range
    /// </summary>
    Task<IEnumerable<LeaveRequest>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get overlapping leave requests for an employee
    /// </summary>
    Task<IEnumerable<LeaveRequest>> GetOverlappingRequestsAsync(Guid employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get team leave calendar (all approved leaves for employees under a manager)
    /// </summary>
    Task<IEnumerable<LeaveRequest>> GetTeamLeaveCalendarAsync(Guid managerId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

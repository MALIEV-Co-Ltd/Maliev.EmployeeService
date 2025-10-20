using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for LeavePolicy entity
/// </summary>
public interface ILeavePolicyRepository : IRepository<LeavePolicy>
{
    /// <summary>
    /// Get active leave policy for a specific leave type
    /// </summary>
    Task<LeavePolicy?> GetByLeaveTypeAsync(LeaveType leaveType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active leave policies
    /// </summary>
    Task<IEnumerable<LeavePolicy>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get leave policy effective at a specific date
    /// </summary>
    Task<LeavePolicy?> GetByLeaveTypeAndDateAsync(LeaveType leaveType, DateTime effectiveDate, CancellationToken cancellationToken = default);
}

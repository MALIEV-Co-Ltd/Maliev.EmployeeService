using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get pending leave approvals for a manager/approver
/// </summary>
public record GetPendingApprovalsQuery(Guid ApproverId);

/// <summary>
/// Response containing pending approvals
/// </summary>
public record GetPendingApprovalsQueryResult(List<LeaveRequestDto> PendingApprovals);

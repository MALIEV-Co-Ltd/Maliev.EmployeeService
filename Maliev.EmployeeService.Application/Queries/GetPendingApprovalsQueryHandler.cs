using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetPendingApprovalsQuery
/// </summary>
public class GetPendingApprovalsQueryHandler
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public GetPendingApprovalsQueryHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<GetPendingApprovalsQueryResult> HandleAsync(
        GetPendingApprovalsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pendingRequests = await _leaveRequestRepository.GetPendingForApproverAsync(
            query.ApproverId,
            cancellationToken);

        var pendingDtos = pendingRequests.Select(lr => new LeaveRequestDto
        {
            Id = lr.Id,
            EmployeeId = lr.EmployeeId,
            EmployeeName = lr.Employee?.FullName ?? string.Empty,
            LeaveType = lr.LeaveType.ToString(),
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            TotalDays = lr.TotalDays,
            Reason = lr.Reason,
            Status = lr.Status.ToString(),
            ApproverId = lr.ApproverId,
            ApproverName = lr.Approver?.FullName,
            ApprovalDate = lr.ApprovalDate,
            ApprovalComments = lr.ApprovalComments,
            CreatedDate = lr.CreatedDate,
            ModifiedDate = lr.ModifiedDate
        }).ToList();

        return new GetPendingApprovalsQueryResult(pendingDtos);
    }
}

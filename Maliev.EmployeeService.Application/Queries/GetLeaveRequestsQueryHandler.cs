using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetLeaveRequestsQuery
/// </summary>
public class GetLeaveRequestsQueryHandler
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;

    public GetLeaveRequestsQueryHandler(ILeaveRequestRepository leaveRequestRepository)
    {
        _leaveRequestRepository = leaveRequestRepository;
    }

    public async Task<GetLeaveRequestsQueryResult> HandleAsync(
        GetLeaveRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        var leaveRequests = await _leaveRequestRepository.GetByEmployeeIdAsync(
            query.EmployeeId,
            cancellationToken);

        var leaveRequestDtos = leaveRequests.Select(lr => new LeaveRequestDto
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

        return new GetLeaveRequestsQueryResult(leaveRequestDtos);
    }
}

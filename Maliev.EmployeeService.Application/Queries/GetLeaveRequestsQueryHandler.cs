using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetLeaveRequestsQuery
/// </summary>
public class GetLeaveRequestsQueryHandler
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetLeaveRequestsQueryHandler(
        ILeaveRequestRepository leaveRequestRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<GetLeaveRequestsQueryResult> HandleAsync(
        GetLeaveRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have LeaveRead permission for this employee
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.EmployeeId}/leave";
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.LeaveRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view leave requests for this employee");
        }

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

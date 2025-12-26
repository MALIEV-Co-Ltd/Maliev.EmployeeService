using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetPendingApprovalsQuery
/// </summary>
public class GetPendingApprovalsQueryHandler
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetPendingApprovalsQueryHandler(
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

    public async Task<GetPendingApprovalsQueryResult> HandleAsync(
        GetPendingApprovalsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have LeaveRead permission for these approvals
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.ApproverId}/approvals";
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.LeaveRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view these pending approvals");
        }

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

using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetLeaveBalancesQuery
/// </summary>
public class GetLeaveBalancesQueryHandler
{
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetLeaveBalancesQueryHandler(
        ILeaveBalanceRepository leaveBalanceRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _leaveBalanceRepository = leaveBalanceRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<GetLeaveBalancesQueryResult> HandleAsync(
        GetLeaveBalancesQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have LeaveRead permission for this employee
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.EmployeeId}/leave";
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.LeaveRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view leave balances for this employee");
        }

        var year = query.Year ?? DateTime.UtcNow.Year;

        var balances = await _leaveBalanceRepository.GetByEmployeeAndYearAsync(
            query.EmployeeId,
            year,
            cancellationToken);

        var balanceDtos = balances.Select(b => new LeaveBalanceDto
        {
            Id = b.Id,
            EmployeeId = b.EmployeeId,
            LeaveType = b.LeaveType.ToString(),
            Year = b.Year,
            TotalEntitlement = b.TotalEntitlement,
            UsedDays = b.UsedDays,
            PendingDays = b.PendingDays,
            CarryForwardDays = b.CarryForwardDays,
            AvailableDays = b.AvailableDays,
            RemainingDays = b.RemainingDays,
            ExpiryDate = b.ExpiryDate,
            HasExpired = b.HasExpired
        }).ToList();

        return new GetLeaveBalancesQueryResult(balanceDtos);
    }
}

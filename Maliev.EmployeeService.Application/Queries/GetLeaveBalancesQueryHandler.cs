using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetLeaveBalancesQuery
/// </summary>
public class GetLeaveBalancesQueryHandler
{
    private readonly ILeaveBalanceRepository _leaveBalanceRepository;

    public GetLeaveBalancesQueryHandler(ILeaveBalanceRepository leaveBalanceRepository)
    {
        _leaveBalanceRepository = leaveBalanceRepository;
    }

    public async Task<GetLeaveBalancesQueryResult> HandleAsync(
        GetLeaveBalancesQuery query,
        CancellationToken cancellationToken = default)
    {
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

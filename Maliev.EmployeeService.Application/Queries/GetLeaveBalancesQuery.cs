using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get leave balances for an employee
/// </summary>
public record GetLeaveBalancesQuery(Guid EmployeeId, int? Year = null);

/// <summary>
/// Response containing leave balances
/// </summary>
public record GetLeaveBalancesQueryResult(List<LeaveBalanceDto> Balances);

using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get leave requests for an employee
/// </summary>
public record GetLeaveRequestsQuery(Guid EmployeeId);

/// <summary>
/// Response containing leave requests
/// </summary>
public record GetLeaveRequestsQueryResult(List<LeaveRequestDto> LeaveRequests);

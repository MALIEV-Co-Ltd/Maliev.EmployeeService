using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get employee profile by employee ID (User Story 1)
/// </summary>
public record GetEmployeeProfileQuery(Guid EmployeeId);

/// <summary>
/// Response containing employee profile data
/// </summary>
public record GetEmployeeProfileQueryResult(EmployeeProfileDto? Profile);

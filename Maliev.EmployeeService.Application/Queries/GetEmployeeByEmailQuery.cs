using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Queries;

public record GetEmployeeByEmailQuery(string Email);

public record GetEmployeeByEmailResult(EmployeeLookupDto? Employee);

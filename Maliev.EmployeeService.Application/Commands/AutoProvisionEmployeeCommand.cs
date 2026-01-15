using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Commands;

public record AutoProvisionEmployeeCommand(
    string Email,
    string FirstName,
    string LastName,
    string? PictureUrl
);

namespace Maliev.EmployeeService.Application.Commands;

public record UpdateEmployeeCommand(
    Guid EmployeeId,
    string? Title,
    string? Status,
    string? Phone);

public record UpdateEmployeeCommandResult(bool Success, string? ErrorMessage = null);

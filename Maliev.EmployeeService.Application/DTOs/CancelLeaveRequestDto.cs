namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for cancelling a leave request
/// </summary>
public class CancelLeaveRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

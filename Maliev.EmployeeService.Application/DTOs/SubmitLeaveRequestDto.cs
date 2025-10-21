namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for submitting a new leave request
/// </summary>
public class SubmitLeaveRequestDto
{
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

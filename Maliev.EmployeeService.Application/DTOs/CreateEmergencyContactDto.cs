namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for creating a new emergency contact (User Story 1)
/// </summary>
public class CreateEmergencyContactDto
{
    public string ContactName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int PriorityOrder { get; set; } = 1;
}

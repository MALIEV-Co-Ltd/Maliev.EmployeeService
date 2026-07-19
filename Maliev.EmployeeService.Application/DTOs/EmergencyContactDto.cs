namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for emergency contact information (User Story 1)
/// </summary>
public class EmergencyContactDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int PriorityOrder { get; set; }
}

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for updating employee profile (User Story 1)
/// Employees can only update limited fields - personal email, mobile phone, and preferred name
/// </summary>
public class UpdateEmployeeProfileDto
{
    /// <summary>
    /// Personal email address (optional)
    /// </summary>
    public string? PersonalEmail { get; set; }

    /// <summary>
    /// Mobile phone number (optional)
    /// </summary>
    public string? MobilePhone { get; set; }

    /// <summary>
    /// Preferred name/nickname (optional)
    /// </summary>
    public string? PreferredName { get; set; }
}

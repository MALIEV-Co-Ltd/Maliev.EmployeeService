using System.ComponentModel.DataAnnotations;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for creating a new emergency contact (User Story 1)
/// </summary>
public class CreateEmergencyContactDto
{
    /// <summary>
    /// The full name of the emergency contact.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// The relationship of the contact to the employee.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Relationship { get; set; } = string.Empty;

    /// <summary>
    /// The phone number of the emergency contact.
    /// </summary>
    [Required]
    [Phone]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the emergency contact.
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// The priority order of the contact.
    /// </summary>
    [Range(1, 10)]
    public int PriorityOrder { get; set; } = 1;
}

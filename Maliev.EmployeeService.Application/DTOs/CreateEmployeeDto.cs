using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for creating a new employee record
/// </summary>
public class CreateEmployeeDto
{
    // Required fields
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string WorkEmail { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime StartDate { get; set; }
    public EmploymentType EmploymentType { get; set; }

    // Optional fields
    public string? PreferredName { get; set; }
    public string? Nationality { get; set; }
    public string? PersonalEmail { get; set; }
    public string? MobilePhone { get; set; }
    public string? JobTitle { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public string? WorkLocation { get; set; }
    public int? WorkLocationId { get; set; } // Reference to Career Service work location catalog
    public DateTime? ProbationEndDate { get; set; }
    public string? NationalId { get; set; } // Will be encrypted

    // Career Service integration
    public Guid? JobApplicationId { get; set; }
}

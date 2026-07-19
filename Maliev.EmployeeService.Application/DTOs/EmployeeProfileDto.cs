namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for employee profile information (User Story 1)
/// </summary>
public class EmployeeProfileDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;

    // Legal Name
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string FullName { get; set; } = string.Empty;

    // Personal Information
    public string? PreferredName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Nationality { get; set; }

    // Contact Information
    public string WorkEmail { get; set; } = string.Empty;
    public string? PersonalEmail { get; set; }
    public string? MobilePhone { get; set; }

    // Employment Information
    public string EmploymentType { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? WorkLocation { get; set; }

    // Employment Dates
    public DateTime StartDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public int? TenureInYears { get; set; }

    // Department and Manager
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }

    // Emergency Contacts
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();
}

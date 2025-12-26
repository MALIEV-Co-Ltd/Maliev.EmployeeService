using System.ComponentModel.DataAnnotations;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for creating a new employee record
/// </summary>
public class CreateEmployeeDto
{
    /// <summary>
    /// The employee's unique identification number.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// The employee's first name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// The employee's last name.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// The employee's middle name.
    /// </summary>
    [StringLength(100)]
    public string? MiddleName { get; set; }

    /// <summary>
    /// The employee's work email address.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string WorkEmail { get; set; } = string.Empty;

    /// <summary>
    /// The employee's date of birth.
    /// </summary>
    [Required]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// The employee's start date of employment.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The type of employment.
    /// </summary>
    public EmploymentType EmploymentType { get; set; }

    /// <summary>
    /// The employee's preferred name.
    /// </summary>
    public string? PreferredName { get; set; }

    /// <summary>
    /// The employee's nationality.
    /// </summary>
    [StringLength(50)]
    public string? Nationality { get; set; }

    /// <summary>
    /// The employee's personal email address.
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string? PersonalEmail { get; set; }

    /// <summary>
    /// The employee's mobile phone number.
    /// </summary>
    [StringLength(20)]
    public string? MobilePhone { get; set; }

    /// <summary>
    /// The employee's job title.
    /// </summary>
    [StringLength(200)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// The ID of the department the employee belongs to.
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// The ID of the employee's manager.
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// The employee's work location.
    /// </summary>
    [StringLength(200)]
    public string? WorkLocation { get; set; }

    /// <summary>
    /// Reference to Career Service work location catalog.
    /// </summary>
    public int? WorkLocationId { get; set; }

    /// <summary>
    /// The end date of the employee's probation period.
    /// </summary>
    public DateTime? ProbationEndDate { get; set; }

    /// <summary>
    /// The employee's national ID number (will be encrypted).
    /// </summary>
    [StringLength(13, MinimumLength = 13)]
    [RegularExpression(@"^\d{13}$")]
    public string? NationalId { get; set; }

    /// <summary>
    /// The ID of the job application in the Career Service.
    /// </summary>
    public Guid? JobApplicationId { get; set; }
}

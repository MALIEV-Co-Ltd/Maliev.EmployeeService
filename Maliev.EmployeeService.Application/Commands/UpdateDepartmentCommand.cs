using System.ComponentModel.DataAnnotations;
using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to update a department.
/// </summary>
public class UpdateDepartmentCommand
{
    /// <summary>
    /// The ID of the department to update.
    /// </summary>
    [Required]
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// The name of the department.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the department.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The ID of the parent department.
    /// </summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>
    /// The ID of the department head.
    /// </summary>
    public Guid? DepartmentHeadId { get; set; }

    /// <summary>
    /// The cost center of the department.
    /// </summary>
    [StringLength(50)]
    public string? CostCenter { get; set; }

    /// <summary>
    /// The headcount limit of the department.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? HeadcountLimit { get; set; }

    /// <summary>
    /// Indicates if the department is active.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// The result of updating a department.
/// </summary>
public class UpdateDepartmentResult
{
    /// <summary>
    /// Indicates if the update was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The error message if the update was not successful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// A list of warnings from the update process.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

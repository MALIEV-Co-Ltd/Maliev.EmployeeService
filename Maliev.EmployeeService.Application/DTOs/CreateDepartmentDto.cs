using System.ComponentModel.DataAnnotations;

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for creating a new department
/// </summary>
public class CreateDepartmentDto
{
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
    [Range(1, 1000)]
    public int? HeadcountLimit { get; set; }
}

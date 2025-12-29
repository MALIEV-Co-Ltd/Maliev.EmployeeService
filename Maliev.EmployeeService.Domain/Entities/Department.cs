using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Department entity representing an organizational unit within the company hierarchy.
/// </summary>
public class Department : Entity
{
    /// <summary>
    /// Gets or sets the name of the department.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the department.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the parent department in the organizational hierarchy.
    /// </summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the employee who heads the department.
    /// </summary>
    public Guid? DepartmentHeadId { get; set; }

    /// <summary>
    /// Gets or sets the cost center associated with the department for financial tracking.
    /// </summary>
    public string? CostCenter { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed headcount for the department.
    /// </summary>
    public int? HeadcountLimit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the department is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the parent department entity.
    /// </summary>
    public Department? ParentDepartment { get; set; }

    /// <summary>
    /// Gets or sets the collection of sub-departments within this department.
    /// </summary>
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();

    /// <summary>
    /// Gets or sets the employee who is the head of this department.
    /// </summary>
    public Employee? DepartmentHead { get; set; }

    /// <summary>
    /// Gets or sets the collection of employees currently assigned to this department.
    /// </summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    /// <summary>
    /// Gets a value indicating whether this is a root department (has no parent).
    /// </summary>
    public bool IsRootDepartment => !ParentDepartmentId.HasValue;

    /// <summary>
    /// Gets a value indicating whether this department has any sub-departments.
    /// </summary>
    public bool HasSubDepartments => SubDepartments.Any();

    /// <summary>
    /// Gets the current headcount of active employees in the department.
    /// </summary>
    public int CurrentHeadcount => Employees.Count(e => e.IsActive);

    /// <summary>
    /// Gets a value indicating whether the department is reaching 80% of its headcount limit.
    /// </summary>
    public bool IsApproachingHeadcountLimit => HeadcountLimit.HasValue &&
                                                 CurrentHeadcount >= (HeadcountLimit.Value * 0.8);

    /// <summary>
    /// Gets a value indicating whether the department has reached or exceeded its headcount limit.
    /// </summary>
    public bool IsAtHeadcountLimit => HeadcountLimit.HasValue &&
                                       CurrentHeadcount >= HeadcountLimit.Value;
}
using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Department entity representing an organizational unit within the company hierarchy
/// </summary>
public class Department : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Hierarchical Structure
    public Guid? ParentDepartmentId { get; set; }

    // Department Leadership
    public Guid? DepartmentHeadId { get; set; }

    // Organizational Details
    public string? CostCenter { get; set; }
    public int? HeadcountLimit { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public Employee? DepartmentHead { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    // Computed Properties
    public bool IsRootDepartment => !ParentDepartmentId.HasValue;
    public bool HasSubDepartments => SubDepartments.Any();
    public int CurrentHeadcount => Employees.Count(e => e.IsActive);
    public bool IsApproachingHeadcountLimit => HeadcountLimit.HasValue &&
                                                 CurrentHeadcount >= (HeadcountLimit.Value * 0.8);
    public bool IsAtHeadcountLimit => HeadcountLimit.HasValue &&
                                       CurrentHeadcount >= HeadcountLimit.Value;
}

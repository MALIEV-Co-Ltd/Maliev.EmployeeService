namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// Department data transfer object for list views
/// </summary>
public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public string? ParentDepartmentName { get; set; }
    public Guid? DepartmentHeadId { get; set; }
    public string? DepartmentHeadName { get; set; }
    public string? CostCenter { get; set; }
    public int? HeadcountLimit { get; set; }
    public int CurrentHeadcount { get; set; }
    public bool IsActive { get; set; }

    // Computed Properties
    public bool IsRootDepartment { get; set; }
    public bool HasSubDepartments { get; set; }
    public bool IsAtHeadcountLimit { get; set; }
    public bool IsApproachingHeadcountLimit { get; set; }
}

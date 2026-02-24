namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// Department detail data transfer object with subdepartments and employees
/// </summary>
public class DepartmentDetailDto
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

    // Navigation Properties
    public List<DepartmentDto> SubDepartments { get; set; } = new();
    public List<EmployeeSummaryDto> Employees { get; set; } = new();
}

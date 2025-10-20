namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for creating a new department
/// </summary>
public class CreateDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Guid? DepartmentHeadId { get; set; }
    public string? CostCenter { get; set; }
    public int? HeadcountLimit { get; set; }
}

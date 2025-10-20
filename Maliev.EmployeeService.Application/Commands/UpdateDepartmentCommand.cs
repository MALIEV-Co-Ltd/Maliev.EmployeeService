using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Commands;

public class UpdateDepartmentCommand
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Guid? DepartmentHeadId { get; set; }
    public string? CostCenter { get; set; }
    public int? HeadcountLimit { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateDepartmentResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
}

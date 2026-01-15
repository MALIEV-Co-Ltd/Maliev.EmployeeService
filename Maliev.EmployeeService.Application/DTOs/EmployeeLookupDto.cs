namespace Maliev.EmployeeService.Application.DTOs;

public class EmployeeLookupDto
{
    public Guid Id { get; set; }
    public Guid PrincipalId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? DepartmentName { get; set; }
    public string? JobTitle { get; set; }
    public Guid? ManagerId { get; set; }
}

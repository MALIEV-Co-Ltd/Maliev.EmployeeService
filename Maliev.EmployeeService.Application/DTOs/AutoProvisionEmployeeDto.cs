namespace Maliev.EmployeeService.Application.DTOs;

public class AutoProvisionEmployeeDto
{
    public Guid Id { get; set; }
    public Guid PrincipalId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsNewEmployee { get; set; }
}

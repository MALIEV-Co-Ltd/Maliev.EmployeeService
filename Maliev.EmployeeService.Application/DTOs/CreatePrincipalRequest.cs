namespace Maliev.EmployeeService.Application.DTOs;

public class CreatePrincipalRequest
{
    public string? Email { get; set; }
    public string? LinkedService { get; set; } = "EmployeeService";
    public Guid? LinkedEntityId { get; set; }
}

namespace Maliev.EmployeeService.Domain.Entities;

public class OffboardingTask
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

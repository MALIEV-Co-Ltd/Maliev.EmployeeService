namespace Maliev.EmployeeService.Domain.Entities;

public class Training
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CompletionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

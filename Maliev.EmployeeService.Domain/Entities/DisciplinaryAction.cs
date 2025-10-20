namespace Maliev.EmployeeService.Domain.Entities;

public class DisciplinaryAction
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

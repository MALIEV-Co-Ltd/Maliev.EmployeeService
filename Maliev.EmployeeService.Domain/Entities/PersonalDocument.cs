namespace Maliev.EmployeeService.Domain.Entities;

public class PersonalDocument
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

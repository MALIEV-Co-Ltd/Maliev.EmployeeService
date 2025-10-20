namespace Maliev.EmployeeService.Domain.Entities;

public class EmploymentHistory
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

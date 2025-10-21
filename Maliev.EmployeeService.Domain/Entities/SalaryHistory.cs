namespace Maliev.EmployeeService.Domain.Entities;

public class SalaryHistory
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

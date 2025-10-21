namespace Maliev.EmployeeService.Domain.Entities;

public class EmployeeBenefit
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid BenefitId { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

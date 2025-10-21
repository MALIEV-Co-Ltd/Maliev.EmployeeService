namespace Maliev.EmployeeService.Domain.Entities;

public class ExitInterview
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime InterviewDate { get; set; }
    public Guid ConductedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

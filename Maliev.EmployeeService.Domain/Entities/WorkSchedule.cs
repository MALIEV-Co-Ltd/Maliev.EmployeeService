namespace Maliev.EmployeeService.Domain.Entities;

public class WorkSchedule
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
}

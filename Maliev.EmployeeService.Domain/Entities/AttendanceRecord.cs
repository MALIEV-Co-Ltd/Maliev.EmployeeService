namespace Maliev.EmployeeService.Domain.Entities;

public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

namespace Maliev.EmployeeService.Domain.Entities;

public class Incident
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime IncidentDate { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

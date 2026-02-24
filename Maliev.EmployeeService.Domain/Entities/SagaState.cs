namespace Maliev.EmployeeService.Domain.Entities;

public class SagaState
{
    public Guid CorrelationId { get; set; }
    public string SagaType { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

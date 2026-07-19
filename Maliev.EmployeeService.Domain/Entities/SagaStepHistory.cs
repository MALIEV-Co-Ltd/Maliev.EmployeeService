namespace Maliev.EmployeeService.Domain.Entities;

public class SagaStepHistory
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

namespace Maliev.EmployeeService.Application.Events;

/// <summary>
/// Event published when a new team is created (User Story 5)
/// </summary>
public record TeamCreatedEvent
{
    public Guid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
    public string TeamType { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? TeamLeadId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid CreatedBy { get; init; }
}

namespace Maliev.EmployeeService.Application.Events;

/// <summary>
/// Event published when a team is updated (User Story 5)
/// </summary>
public record TeamUpdatedEvent
{
    public Guid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
    public string TeamType { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? TeamLeadId { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid UpdatedBy { get; init; }
    public Dictionary<string, object?> Changes { get; init; } = new();
}

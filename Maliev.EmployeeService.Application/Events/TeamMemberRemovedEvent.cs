namespace Maliev.EmployeeService.Application.Events;

/// <summary>
/// Event published when a member is removed from a team (User Story 5)
/// </summary>
public record TeamMemberRemovedEvent
{
    public Guid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public string EmployeeNumber { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime RemovedAt { get; init; }
    public Guid RemovedBy { get; init; }
}

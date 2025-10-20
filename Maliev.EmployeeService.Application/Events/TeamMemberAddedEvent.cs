namespace Maliev.EmployeeService.Application.Events;

/// <summary>
/// Event published when a member is added to a team (User Story 5)
/// </summary>
public record TeamMemberAddedEvent
{
    public Guid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public string EmployeeNumber { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public DateTime AddedAt { get; init; }
    public Guid AddedBy { get; init; }
}

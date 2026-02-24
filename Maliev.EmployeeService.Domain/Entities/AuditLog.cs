namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Represents a single audit log entry for tracking changes to system data.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public Guid LogId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the change occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user who made the change.
    /// </summary>
    public Guid? PrincipalId { get; set; }

    /// <summary>
    /// Gets or sets the type of entity that was changed (e.g., Employee, Department).
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the specific entity that was changed.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Gets or sets the type of action performed (e.g., Create, Update, Delete).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-serialized state of the entity before the change.
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized state of the entity after the change.
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the change was initiated.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the purpose or reason for the change, for compliance tracking.
    /// </summary>
    public string? Purpose { get; set; }
}

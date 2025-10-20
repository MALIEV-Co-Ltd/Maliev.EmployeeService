namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Immutable audit log entity for tracking all data changes
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid LogId { get; set; }

    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User ID who performed the action
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Type of entity that was modified (e.g., "Employee", "LeaveRequest")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity that was modified
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Action performed (Create, Update, Delete)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// JSON representation of old values (for Update/Delete actions)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON representation of new values (for Create/Update actions)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address of the client making the request
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Business purpose/reason for the change (GDPR compliance)
    /// </summary>
    public string? Purpose { get; set; }
}

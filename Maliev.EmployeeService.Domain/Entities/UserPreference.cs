using Maliev.EmployeeService.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Stores user-specific preferences for UI customization.
/// </summary>
[Table("user_preferences")]
public class UserPreference : Entity
{
    /// <summary>
    /// The user's principal ID (PK part 1).
    /// </summary>
    public Guid PrincipalId { get; set; }

    /// <summary>
    /// The scope of the preference (e.g., "dashboard", "notifications") (PK part 2).
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// JSON payload of the preference data.
    ///Stored as JSONB in PostgreSQL.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string PreferenceData { get; set; } = "{}";

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

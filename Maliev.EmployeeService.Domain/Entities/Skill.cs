using Maliev.EmployeeService.Domain.Common;

namespace Maliev.EmployeeService.Domain.Entities;

/// <summary>
/// Employee skill for skills matrix tracking
/// </summary>
public class Skill : Entity
{
    /// <summary>
    /// Employee who has this skill
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Navigation property to Employee
    /// </summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Name of the skill
    /// </summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    /// Proficiency level (1-5, or custom scale)
    /// </summary>
    public int ProficiencyLevel { get; set; }

    /// <summary>
    /// Date when skill was last assessed
    /// </summary>
    public DateTime LastAssessedDate { get; set; }

    /// <summary>
    /// Indicates if this is a development area for the employee
    /// </summary>
    public bool IsDevelopmentArea { get; set; }

    /// <summary>
    /// Additional notes about the skill assessment
    /// </summary>
    public string? Notes { get; set; }
}

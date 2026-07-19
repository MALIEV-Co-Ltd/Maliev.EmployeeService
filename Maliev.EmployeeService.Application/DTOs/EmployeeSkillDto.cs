namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for employee skill information
/// </summary>
public class EmployeeSkillDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int ProficiencyLevel { get; set; }
    public DateTime LastAssessedDate { get; set; }
    public bool IsDevelopmentArea { get; set; }
    public string? Notes { get; set; }
}

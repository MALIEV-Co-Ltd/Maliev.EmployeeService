namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for creating a skill record
/// </summary>
public class CreateSkillDto
{
    public string SkillName { get; set; } = string.Empty;
    public int ProficiencyLevel { get; set; }
    public bool IsDevelopmentArea { get; set; }
    public string? Notes { get; set; }
}

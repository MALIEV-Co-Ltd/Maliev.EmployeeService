namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for updating a skill record
/// </summary>
public class UpdateSkillDto
{
    public int ProficiencyLevel { get; set; }
    public bool IsDevelopmentArea { get; set; }
    public string? Notes { get; set; }
}

namespace Maliev.EmployeeService.Application.DTOs.CareerService;

/// <summary>
/// Skill information from Career Service catalog
/// </summary>
public class SkillDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
}

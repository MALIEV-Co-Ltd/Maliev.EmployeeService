namespace Maliev.EmployeeService.Application.DTOs.CareerService;

/// <summary>
/// Work location information from Career Service catalog
/// </summary>
public class WorkLocationDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; }
}

namespace Maliev.EmployeeService.Application.DTOs.CareerService;

/// <summary>
/// Job posting data from Career Service
/// </summary>
public class JobPostingDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PostedDate { get; set; }
    public DateTime? ClosingDate { get; set; }
}

namespace Maliev.EmployeeService.Application.DTOs.CareerService;

/// <summary>
/// Job application data from Career Service
/// </summary>
public class JobApplicationDto
{
    public Guid Id { get; set; }
    public Guid JobPostingId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    public Guid? EmployeeId { get; set; } // Set when applicant becomes employee
}

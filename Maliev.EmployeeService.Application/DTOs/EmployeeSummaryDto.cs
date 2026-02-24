namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// Employee summary data transfer object for list views
/// </summary>
public class EmployeeSummaryDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
}

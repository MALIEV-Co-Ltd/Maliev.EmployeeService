namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// Background job status information for monitoring
/// </summary>
public class BackgroundJobStatusDto
{
    public string JobName { get; set; } = string.Empty;
    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string? LastError { get; set; }
    public string Status { get; set; } = "Unknown";
}

using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Service for tracking and retrieving background job execution status
/// </summary>
public interface IBackgroundJobStatusService
{
    /// <summary>
    /// Record a successful job execution
    /// </summary>
    void RecordSuccess(string jobName, DateTime executionTime, DateTime? nextRunTime = null);

    /// <summary>
    /// Record a failed job execution
    /// </summary>
    void RecordFailure(string jobName, DateTime executionTime, string errorMessage, DateTime? nextRunTime = null);

    /// <summary>
    /// Get status for all background jobs
    /// </summary>
    IEnumerable<BackgroundJobStatusDto> GetAllJobStatuses();

    /// <summary>
    /// Get status for a specific background job
    /// </summary>
    BackgroundJobStatusDto? GetJobStatus(string jobName);
}

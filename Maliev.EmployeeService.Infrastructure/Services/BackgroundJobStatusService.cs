using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using System.Collections.Concurrent;

namespace Maliev.EmployeeService.Infrastructure.Services;

/// <summary>
/// In-memory implementation of background job status tracking
/// </summary>
public class BackgroundJobStatusService : IBackgroundJobStatusService
{
    private readonly ConcurrentDictionary<string, JobStatus> _jobStatuses = new();

    public void RecordSuccess(string jobName, DateTime executionTime, DateTime? nextRunTime = null)
    {
        _jobStatuses.AddOrUpdate(
            jobName,
            _ => new JobStatus
            {
                JobName = jobName,
                LastRunTime = executionTime,
                NextRunTime = nextRunTime,
                SuccessCount = 1,
                FailureCount = 0,
                LastError = null
            },
            (_, existing) =>
            {
                existing.LastRunTime = executionTime;
                existing.NextRunTime = nextRunTime;
                existing.SuccessCount++;
                existing.LastError = null;
                return existing;
            });
    }

    public void RecordFailure(string jobName, DateTime executionTime, string errorMessage, DateTime? nextRunTime = null)
    {
        _jobStatuses.AddOrUpdate(
            jobName,
            _ => new JobStatus
            {
                JobName = jobName,
                LastRunTime = executionTime,
                NextRunTime = nextRunTime,
                SuccessCount = 0,
                FailureCount = 1,
                LastError = errorMessage
            },
            (_, existing) =>
            {
                existing.LastRunTime = executionTime;
                existing.NextRunTime = nextRunTime;
                existing.FailureCount++;
                existing.LastError = errorMessage;
                return existing;
            });
    }

    public IEnumerable<BackgroundJobStatusDto> GetAllJobStatuses()
    {
        return _jobStatuses.Values.Select(MapToDto);
    }

    public BackgroundJobStatusDto? GetJobStatus(string jobName)
    {
        return _jobStatuses.TryGetValue(jobName, out var status)
            ? MapToDto(status)
            : null;
    }

    private static BackgroundJobStatusDto MapToDto(JobStatus status)
    {
        var now = DateTime.UtcNow;
        var statusText = "Unknown";

        if (status.LastRunTime.HasValue)
        {
            statusText = string.IsNullOrEmpty(status.LastError) ? "Healthy" : "Failed";
        }
        else if (status.NextRunTime.HasValue && status.NextRunTime.Value > now)
        {
            statusText = "Scheduled";
        }

        return new BackgroundJobStatusDto
        {
            JobName = status.JobName,
            LastRunTime = status.LastRunTime,
            NextRunTime = status.NextRunTime,
            SuccessCount = status.SuccessCount,
            FailureCount = status.FailureCount,
            LastError = status.LastError,
            Status = statusText
        };
    }

    private class JobStatus
    {
        public string JobName { get; set; } = string.Empty;
        public DateTime? LastRunTime { get; set; }
        public DateTime? NextRunTime { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public string? LastError { get; set; }
    }
}

using System.Text.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetBulkJobStatusQuery
/// Retrieves the current status of a bulk operation job
/// User Story 12 - Bulk Operations
/// </summary>
public class GetBulkJobStatusQueryHandler
{
    private readonly IBulkJobRepository _bulkJobRepository;

    public GetBulkJobStatusQueryHandler(IBulkJobRepository bulkJobRepository)
    {
        _bulkJobRepository = bulkJobRepository;
    }

    /// <summary>
    /// Handles the query to get bulk job status
    /// </summary>
    public async Task<BulkJobStatusDto?> HandleAsync(
        GetBulkJobStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var job = await _bulkJobRepository.GetByJobIdAsync(query.JobId, cancellationToken);

        if (job == null)
        {
            return null;
        }

        // Parse errors from JSON if available
        var errors = new List<string>();
        if (!string.IsNullOrEmpty(job.Errors))
        {
            try
            {
                errors = JsonSerializer.Deserialize<List<string>>(job.Errors) ?? new List<string>();
            }
            catch
            {
                // If deserialization fails, treat the whole string as a single error
                errors = new List<string> { job.Errors };
            }
        }

        return new BulkJobStatusDto
        {
            JobId = job.JobId,
            JobType = job.JobType,
            Status = job.Status.ToString(),
            TotalRecords = job.TotalRecords,
            SuccessfulRecords = job.SuccessfulRecords,
            FailedRecords = job.FailedRecords,
            ProgressPercentage = job.ProgressPercentage,
            Errors = errors,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            InitiatedByUserId = job.InitiatedByUserId,
            ResultData = job.ResultData
        };
    }
}

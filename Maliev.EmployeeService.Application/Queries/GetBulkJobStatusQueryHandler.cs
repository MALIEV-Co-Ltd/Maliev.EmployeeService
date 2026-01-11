using System.Text.Json;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetBulkJobStatusQuery
/// Retrieves the current status of a bulk operation job
/// User Story 12 - Bulk Operations
/// </summary>
public class GetBulkJobStatusQueryHandler
{
    private readonly IBulkJobRepository _bulkJobRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetBulkJobStatusQueryHandler(
        IBulkJobRepository bulkJobRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _bulkJobRepository = bulkJobRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Handles the query to get bulk job status
    /// </summary>
    public async Task<BulkJobStatusDto?> HandleAsync(
        GetBulkJobStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have AdminBackgroundJobs permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/bulk-jobs/{query.JobId}";
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.AdminBackgroundJobs, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view bulk job status");
        }

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
            InitiatedByPrincipalId = job.InitiatedByPrincipalId,
            ResultData = job.ResultData
        };
    }
}

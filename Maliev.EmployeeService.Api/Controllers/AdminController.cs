using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.Authorization;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Administrative endpoints for system monitoring and management
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/admin")]
[RequirePermission(EmployeePermissions.AdminBackgroundJobs)]
public class AdminController : ControllerBase
{
    private readonly IBackgroundJobStatusService _backgroundJobStatusService;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class
    /// </summary>
    public AdminController(
        IBackgroundJobStatusService backgroundJobStatusService,
        ILogger<AdminController> logger)
    {
        _backgroundJobStatusService = backgroundJobStatusService;
        _logger = logger;
    }

    /// <summary>
    /// Get status of all background jobs
    /// </summary>
    /// <returns>Background job status information</returns>
    /// <response code="200">Returns the status of all background jobs</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have admin privileges</response>
    [HttpGet("background-jobs/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetBackgroundJobsStatus()
    {
        _logger.LogInformation("Admin requested background job status");

        var jobStatuses = _backgroundJobStatusService.GetAllJobStatuses();

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            Jobs = jobStatuses
        });
    }

    /// <summary>
    /// Get status of a specific background job
    /// </summary>
    /// <param name="jobName">The name of the background job</param>
    /// <returns>Background job status information</returns>
    /// <response code="200">Returns the status of the specified background job</response>
    /// <response code="404">If the specified job is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have admin privileges</response>
    [HttpGet("background-jobs/status/{jobName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetBackgroundJobStatus(string jobName)
    {
        _logger.LogInformation("Admin requested status for background job: {JobName}", jobName);

        var jobStatus = _backgroundJobStatusService.GetJobStatus(jobName);

        if (jobStatus == null)
        {
            return NotFound(new { Message = $"Background job '{jobName}' not found" });
        }

        return Ok(jobStatus);
    }
}

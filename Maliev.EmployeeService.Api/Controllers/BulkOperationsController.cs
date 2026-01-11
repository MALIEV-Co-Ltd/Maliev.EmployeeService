using Asp.Versioning;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Aspire.ServiceDefaults.IAM;
using System.Security.Claims;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Controller for bulk operations and data import/export
/// User Story 12 - Reporting, Analytics and Bulk Operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/bulk")]
[Authorize]
public class BulkOperationsController : ControllerBase
{
    private readonly ExportEmployeesQueryHandler _exportHandler;
    private readonly GetBulkJobStatusQueryHandler _statusHandler;
    private readonly ImportEmployeesCommandHandler _importHandler;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsController"/> class
    /// </summary>
    public BulkOperationsController(
        ExportEmployeesQueryHandler exportHandler,
        GetBulkJobStatusQueryHandler statusHandler,
        ImportEmployeesCommandHandler importHandler,
        ICurrentUserService currentUserService)
    {
        _exportHandler = exportHandler;
        _statusHandler = statusHandler;
        _importHandler = importHandler;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Export employees to CSV format with privacy controls
    /// </summary>
    /// <param name="departmentId">Optional department filter</param>
    /// <param name="employmentStatus">Optional employment status filter</param>
    /// <param name="includeTerminated">Include terminated employees (default: false)</param>
    /// <param name="includePersonalData">Include personal contact information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CSV file download</returns>
    [HttpPost("employees/export")]
    [RequirePermission(EmployeePermissions.ReportsGenerate)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ExportEmployees(
        [FromQuery] Guid? departmentId,
        [FromQuery] Domain.Enums.EmploymentStatus? employmentStatus,
        [FromQuery] bool includeTerminated = false,
        [FromQuery] bool includePersonalData = true,
        CancellationToken cancellationToken = default)
    {
        var query = new ExportEmployeesQuery
        {
            DepartmentId = departmentId,
            EmploymentStatus = employmentStatus,
            IncludeTerminated = includeTerminated,
            IncludeSalaryData = false, // Compensation migrated
            IncludePersonalData = includePersonalData
        };

        var result = await _exportHandler.HandleAsync(query, cancellationToken);

        var bytes = Encoding.UTF8.GetBytes(result.CsvContent);
        return File(bytes, "text/csv", result.FileName);
    }

    /// <summary>
    /// Get the status of a bulk operation job
    /// </summary>
    /// <param name="jobId">Job ID to query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job status information</returns>
    [HttpGet("jobs/{jobId}/status")]
    [RequirePermission(EmployeePermissions.ReportsView)]
    [ProducesResponseType(typeof(BulkJobStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkJobStatusDto>> GetJobStatus(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBulkJobStatusQuery { JobId = jobId };
        var status = await _statusHandler.HandleAsync(query, cancellationToken);

        if (status == null)
        {
            return NotFound(new { message = $"Job {jobId} not found" });
        }

        return Ok(status);
    }

    /// <summary>
    /// Import employees from CSV file
    /// </summary>
    /// <param name="file">CSV file to upload</param>
    /// <param name="skipInvalidRows">Skip validation errors and import valid rows only</param>
    /// <param name="dryRun">Dry run mode - validate only without importing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import job information</returns>
    [HttpPost("employees/import")]
    [RequirePermission(EmployeePermissions.ProfilesCreate)]
    [ProducesResponseType(typeof(ImportEmployeesResultDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportEmployeesResultDto>> ImportEmployees(
        IFormFile file,
        [FromQuery] bool skipInvalidRows = false,
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "CSV file is required" });
        }

        if (!file.ContentType.Contains("csv") && !file.ContentType.Contains("text"))
        {
            return BadRequest(new { message = "File must be CSV format" });
        }

        // Read CSV content
        string csvContent;
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            csvContent = await reader.ReadToEndAsync(cancellationToken);
        }

        var command = new ImportEmployeesCommand
        {
            CsvContent = csvContent,
            SkipInvalidRows = skipInvalidRows,
            DryRun = dryRun,
            InitiatedByPrincipalId = _currentUserService.PrincipalId ?? Guid.Empty
        };

        var jobId = await _importHandler.HandleAsync(command, cancellationToken);

        var result = new ImportEmployeesResultDto
        {
            JobId = jobId,
            Message = dryRun
                ? "Validation completed. Check job status for results."
                : "Import started. Check job status for progress.",
            TotalRows = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1, // Exclude header
            StatusUrl = $"/v1/bulk/jobs/{jobId}/status"
        };

        return AcceptedAtAction(nameof(GetJobStatus), new { jobId }, result);
    }
}

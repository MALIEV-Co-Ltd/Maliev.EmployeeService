using Asp.Versioning;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Work authorization and visa tracking endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/work-authorization")]
[Authorize]
public class WorkAuthorizationController : ControllerBase
{
    private readonly RecordWorkAuthorizationCommandHandler _recordAuthorizationHandler;
    private readonly UpdateWorkAuthorizationCommandHandler _updateAuthorizationHandler;
    private readonly GetWorkAuthorizationQueryHandler _getAuthorizationHandler;
    private readonly GetWorkAuthorizationComplianceReportQueryHandler _complianceReportHandler;
    private readonly IWorkAuthorizationRepository _workAuthorizationRepository;
    private readonly ILogger<WorkAuthorizationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkAuthorizationController"/> class
    /// </summary>
    public WorkAuthorizationController(
        RecordWorkAuthorizationCommandHandler recordAuthorizationHandler,
        UpdateWorkAuthorizationCommandHandler updateAuthorizationHandler,
        GetWorkAuthorizationQueryHandler getAuthorizationHandler,
        GetWorkAuthorizationComplianceReportQueryHandler complianceReportHandler,
        IWorkAuthorizationRepository workAuthorizationRepository,
        ILogger<WorkAuthorizationController> logger)
    {
        _recordAuthorizationHandler = recordAuthorizationHandler;
        _updateAuthorizationHandler = updateAuthorizationHandler;
        _getAuthorizationHandler = getAuthorizationHandler;
        _complianceReportHandler = complianceReportHandler;
        _workAuthorizationRepository = workAuthorizationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Record work authorization for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="command">Work authorization details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("employees/{employeeId:guid}")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> RecordWorkAuthorization(
        Guid employeeId,
        [FromBody] RecordWorkAuthorizationCommand command,
        CancellationToken cancellationToken = default)
    {
        command.EmployeeId = employeeId;

        var authorizationId = await _recordAuthorizationHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetWorkAuthorization),
            new { employeeId },
            authorizationId);
    }

    /// <summary>
    /// Get work authorization details for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="includeInactive">Include inactive authorizations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("employees/{employeeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<WorkAuthorizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<WorkAuthorizationDto>>> GetWorkAuthorization(
        Guid employeeId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWorkAuthorizationQuery
        {
            EmployeeId = employeeId,
            IncludeInactive = includeInactive
        };

        var authorizations = await _getAuthorizationHandler.HandleAsync(query, cancellationToken);

        return Ok(authorizations);
    }

    /// <summary>
    /// Update work authorization
    /// </summary>
    /// <param name="authId">Authorization ID</param>
    /// <param name="command">Updated authorization details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("{authId:guid}")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWorkAuthorization(
        Guid authId,
        [FromBody] UpdateWorkAuthorizationCommand command,
        CancellationToken cancellationToken = default)
    {
        command.AuthorizationId = authId;

        var success = await _updateAuthorizationHandler.HandleAsync(command, cancellationToken);

        if (!success)
        {
            return NotFound($"Work authorization {authId} not found");
        }

        _logger.LogInformation("Work authorization {AuthId} updated", authId);
        return NoContent();
    }

    /// <summary>
    /// Get work authorizations expiring within specified days
    /// </summary>
    /// <param name="daysUntilExpiration">Days until expiration threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("expiring")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(IEnumerable<WorkAuthorizationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WorkAuthorizationDto>>> GetExpiringAuthorizations(
        [FromQuery] int daysUntilExpiration = 90,
        CancellationToken cancellationToken = default)
    {
        var authorizations = await _workAuthorizationRepository.GetExpiringAsync(
            daysUntilExpiration,
            cancellationToken);

        var now = DateTime.UtcNow;

        var result = authorizations.Select(a => new WorkAuthorizationDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            AuthorizationType = a.AuthorizationType,
            DocumentNumber = a.DocumentNumber,
            IssueDate = a.IssueDate,
            ExpirationDate = a.ExpirationDate,
            IssuingAuthority = a.IssuingAuthority,
            SponsorshipStatus = a.SponsorshipStatus,
            RightToWorkDocumentId = a.RightToWorkDocumentId,
            Notes = a.Notes,
            IsActive = a.IsActive,
            IsExpired = false,
            DaysUntilExpiration = a.ExpirationDate.HasValue
                ? (int)(a.ExpirationDate.Value - now).TotalDays
                : null
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get work authorization compliance report
    /// </summary>
    /// <param name="daysUntilExpiration">Days until expiration threshold for "expiring soon"</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("compliance-report")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(WorkAuthorizationComplianceReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkAuthorizationComplianceReportDto>> GetComplianceReport(
        [FromQuery] int daysUntilExpiration = 90,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWorkAuthorizationComplianceReportQuery
        {
            DaysUntilExpiration = daysUntilExpiration
        };

        var report = await _complianceReportHandler.HandleAsync(query, cancellationToken);

        return Ok(report);
    }
}

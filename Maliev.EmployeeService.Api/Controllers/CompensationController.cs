using Asp.Versioning;
using Maliev.EmployeeService.Api.Attributes;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Compensation and benefits management controller (User Story 6)
/// Handles salary, bonuses, commissions, and benefits enrollment
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employees/v{version:apiVersion}/employees/{employeeId:guid}")]
[RequireCompensationAccess("HR Compensation and Benefits Administration")]
public class CompensationController : ControllerBase
{
    private readonly GetCompensationDetailsQueryHandler _getDetailsHandler;
    private readonly GetCompensationHistoryQueryHandler _getHistoryHandler;
    private readonly RecordCompensationChangeCommandHandler _recordCompensationHandler;
    private readonly UpdateBenefitsEnrollmentCommandHandler _updateBenefitsHandler;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CompensationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensationController"/> class
    /// </summary>
    public CompensationController(
        GetCompensationDetailsQueryHandler getDetailsHandler,
        GetCompensationHistoryQueryHandler getHistoryHandler,
        RecordCompensationChangeCommandHandler recordCompensationHandler,
        UpdateBenefitsEnrollmentCommandHandler updateBenefitsHandler,
        ICurrentUserService currentUserService,
        ILogger<CompensationController> logger)
    {
        _getDetailsHandler = getDetailsHandler;
        _getHistoryHandler = getHistoryHandler;
        _recordCompensationHandler = recordCompensationHandler;
        _updateBenefitsHandler = updateBenefitsHandler;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get current compensation details for an employee
    /// Authorization: HR Specialist, HR Generalist, System Administrator only
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("compensation")]
    [ProducesResponseType(typeof(CompensationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCompensationDetails(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "User {UserId} accessing compensation details for employee {EmployeeId}",
                _currentUserService.EmployeeId,
                employeeId);

            var query = new GetCompensationDetailsQuery(employeeId);
            var result = await _getDetailsHandler.HandleAsync(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Compensation record not found for employee" });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "Unauthorized access attempt by {UserId} to compensation for employee {EmployeeId}: {Message}",
                _currentUserService.EmployeeId,
                employeeId,
                ex.Message);
            return Forbid();
        }
    }

    /// <summary>
    /// Record a new compensation change for an employee
    /// Authorization: HR Specialist, HR Generalist, System Administrator only
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="compensationDto">Compensation change details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("compensation")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordCompensationChange(
        Guid employeeId,
        [FromBody] RecordCompensationChangeDto compensationDto,
        CancellationToken cancellationToken)
    {
        var command = new RecordCompensationChangeCommand(employeeId, compensationDto);
        var result = await _recordCompensationHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation(
            "Compensation record {CompensationRecordId} created for employee {EmployeeId} by {UserId}",
            result.CompensationRecordId,
            employeeId,
            _currentUserService.EmployeeId);

        return CreatedAtAction(
            nameof(GetCompensationDetails),
            new { employeeId },
            new { id = result.CompensationRecordId, message = "Compensation record created successfully" });
    }

    /// <summary>
    /// Get complete compensation history for an employee
    /// Authorization: HR Specialist, HR Generalist, System Administrator only
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("compensation/history")]
    [ProducesResponseType(typeof(IEnumerable<CompensationDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCompensationHistory(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "User {UserId} accessing compensation history for employee {EmployeeId}",
                _currentUserService.EmployeeId,
                employeeId);

            var query = new GetCompensationHistoryQuery(employeeId);
            var result = await _getHistoryHandler.HandleAsync(query, cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "Unauthorized access attempt by {UserId} to compensation history for employee {EmployeeId}: {Message}",
                _currentUserService.EmployeeId,
                employeeId,
                ex.Message);
            return Forbid();
        }
    }

    /// <summary>
    /// Get current benefits enrollment for an employee
    /// Authorization: HR Specialist, HR Generalist, System Administrator only
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("benefits")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBenefitsEnrollment(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User {UserId} accessing benefits enrollment for employee {EmployeeId}",
            _currentUserService.EmployeeId,
            employeeId);

        // TODO: Implement GetBenefitsEnrollmentQuery
        // For now, return a placeholder
        return NotFound(new { message = "Benefits enrollment not found" });
    }

    /// <summary>
    /// Update benefits enrollment for an employee
    /// Authorization: HR Specialist, HR Generalist, System Administrator only
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="benefitsDto">Benefits enrollment details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("benefits")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBenefitsEnrollment(
        Guid employeeId,
        [FromBody] UpdateBenefitsEnrollmentDto benefitsDto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBenefitsEnrollmentCommand(employeeId, benefitsDto);
        var result = await _updateBenefitsHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation(
            "Benefits enrollment updated for employee {EmployeeId} by {UserId}",
            employeeId,
            _currentUserService.EmployeeId);

        return Ok(new { id = result.EnrollmentId, message = "Benefits enrollment updated successfully" });
    }
}

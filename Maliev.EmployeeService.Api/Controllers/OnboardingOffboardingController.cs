using Asp.Versioning;

using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Onboarding and offboarding workflow management (User Story 10)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employees/v{version:apiVersion}")]
public class OnboardingOffboardingController : ControllerBase
{
    private readonly StartOnboardingCommandHandler _startOnboardingHandler;
    private readonly StartOffboardingCommandHandler _startOffboardingHandler;
    private readonly GetOnboardingStatusQueryHandler _getOnboardingStatusHandler;
    private readonly GetOffboardingStatusQueryHandler _getOffboardingStatusHandler;
    private readonly CompleteOnboardingItemCommandHandler _completeOnboardingItemHandler;
    private readonly CompleteOffboardingItemCommandHandler _completeOffboardingItemHandler;
    private readonly ILogger<OnboardingOffboardingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnboardingOffboardingController"/> class
    /// </summary>
    public OnboardingOffboardingController(
        StartOnboardingCommandHandler startOnboardingHandler,
        StartOffboardingCommandHandler startOffboardingHandler,
        GetOnboardingStatusQueryHandler getOnboardingStatusHandler,
        GetOffboardingStatusQueryHandler getOffboardingStatusHandler,
        CompleteOnboardingItemCommandHandler completeOnboardingItemHandler,
        CompleteOffboardingItemCommandHandler completeOffboardingItemHandler,
        ILogger<OnboardingOffboardingController> logger)
    {
        _startOnboardingHandler = startOnboardingHandler;
        _startOffboardingHandler = startOffboardingHandler;
        _getOnboardingStatusHandler = getOnboardingStatusHandler;
        _getOffboardingStatusHandler = getOffboardingStatusHandler;
        _completeOnboardingItemHandler = completeOnboardingItemHandler;
        _completeOffboardingItemHandler = completeOffboardingItemHandler;
        _logger = logger;
    }

    /// <summary>
    /// Start onboarding workflow for a new employee (HR Specialist, System Admin only)
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("employees/{employeeId:guid}/onboarding/start")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartOnboarding(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new StartOnboardingCommand { EmployeeId = employeeId };
            var result = await _startOnboardingHandler.HandleAsync(command, cancellationToken);

            _logger.LogInformation("Onboarding workflow started for employee {EmployeeId}", employeeId);

            return Ok(new
            {
                employeeId = result,
                message = "Onboarding workflow started successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to start onboarding for employee {EmployeeId}", employeeId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting onboarding for employee {EmployeeId}", employeeId);
            return BadRequest(new { message = "An error occurred while starting onboarding" });
        }
    }

    /// <summary>
    /// Get onboarding status for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Onboarding status with checklist</returns>
    [HttpGet("employees/{employeeId:guid}/onboarding/status")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOnboardingStatus(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetOnboardingStatusQuery { EmployeeId = employeeId };
            var status = await _getOnboardingStatusHandler.HandleAsync(query, cancellationToken);

            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to get onboarding status for employee {EmployeeId}", employeeId);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Complete an onboarding checklist item
    /// </summary>
    /// <param name="itemId">Onboarding checklist item ID</param>
    /// <param name="request">Completion details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPut("onboarding-items/{itemId:guid}/complete")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteOnboardingItem(
        Guid itemId,
        [FromBody] CompleteChecklistItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CompleteOnboardingItemCommand
            {
                ItemId = itemId,
                Notes = request.Notes
            };

            await _completeOnboardingItemHandler.HandleAsync(command, cancellationToken);

            _logger.LogInformation("Onboarding item {ItemId} completed", itemId);

            return Ok(new { message = "Onboarding item completed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to complete onboarding item {ItemId}", itemId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding item {ItemId}", itemId);
            return BadRequest(new { message = "An error occurred while completing the onboarding item" });
        }
    }

    /// <summary>
    /// Start offboarding workflow for an employee (HR Specialist, System Admin only)
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="request">Offboarding details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("employees/{employeeId:guid}/offboarding/start")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StartOffboarding(
        Guid employeeId,
        [FromBody] StartOffboardingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TerminationDate == default)
        {
            return BadRequest(new { message = "Termination date is required" });
        }

        if (string.IsNullOrWhiteSpace(request.TerminationReason))
        {
            return BadRequest(new { message = "Termination reason is required" });
        }

        try
        {
            var command = new StartOffboardingCommand
            {
                EmployeeId = employeeId,
                TerminationDate = request.TerminationDate,
                TerminationReason = request.TerminationReason,
                EligibleForRehire = request.EligibleForRehire
            };

            var result = await _startOffboardingHandler.HandleAsync(command, cancellationToken);

            _logger.LogInformation("Offboarding workflow started for employee {EmployeeId}", employeeId);

            return Ok(new
            {
                employeeId = result,
                message = "Offboarding workflow started successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to start offboarding for employee {EmployeeId}", employeeId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting offboarding for employee {EmployeeId}", employeeId);
            return BadRequest(new { message = "An error occurred while starting offboarding" });
        }
    }

    /// <summary>
    /// Get offboarding status for an employee with paycheck blocking validation
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Offboarding status with checklist and paycheck release status</returns>
    [HttpGet("employees/{employeeId:guid}/offboarding/status")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOffboardingStatus(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetOffboardingStatusQuery { EmployeeId = employeeId };
            var status = await _getOffboardingStatusHandler.HandleAsync(query, cancellationToken);

            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to get offboarding status for employee {EmployeeId}", employeeId);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Complete an offboarding checklist item
    /// </summary>
    /// <param name="itemId">Offboarding checklist item ID</param>
    /// <param name="request">Completion details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPut("offboarding-items/{itemId:guid}/complete")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteOffboardingItem(
        Guid itemId,
        [FromBody] CompleteChecklistItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CompleteOffboardingItemCommand
            {
                ItemId = itemId,
                Notes = request.Notes
            };

            await _completeOffboardingItemHandler.HandleAsync(command, cancellationToken);

            _logger.LogInformation("Offboarding item {ItemId} completed", itemId);

            return Ok(new { message = "Offboarding item completed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to complete offboarding item {ItemId}", itemId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing offboarding item {ItemId}", itemId);
            return BadRequest(new { message = "An error occurred while completing the offboarding item" });
        }
    }
}

/// <summary>
/// Request model for starting offboarding workflow
/// </summary>
public record StartOffboardingRequest(
    DateTime TerminationDate,
    string TerminationReason,
    bool EligibleForRehire);

/// <summary>
/// Request model for completing checklist items
/// </summary>
public record CompleteChecklistItemRequest(string? Notes);

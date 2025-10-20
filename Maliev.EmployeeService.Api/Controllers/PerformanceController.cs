using Asp.Versioning;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Performance Management controller (User Story 7)
/// Manages performance reviews, goals, and feedback
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/performance")]
[Authorize]
public class PerformanceController : ControllerBase
{
    private readonly GetPerformanceReviewsQueryHandler _getReviewsHandler;
    private readonly CreatePerformanceReviewCommandHandler _createReviewHandler;
    private readonly AcknowledgePerformanceReviewCommandHandler _acknowledgeReviewHandler;
    private readonly CreateGoalCommandHandler _createGoalHandler;
    private readonly UpdateGoalProgressCommandHandler _updateGoalProgressHandler;
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(
        GetPerformanceReviewsQueryHandler getReviewsHandler,
        CreatePerformanceReviewCommandHandler createReviewHandler,
        AcknowledgePerformanceReviewCommandHandler acknowledgeReviewHandler,
        CreateGoalCommandHandler createGoalHandler,
        UpdateGoalProgressCommandHandler updateGoalProgressHandler,
        IGoalRepository goalRepository,
        ICurrentUserService currentUserService,
        ILogger<PerformanceController> logger)
    {
        _getReviewsHandler = getReviewsHandler;
        _createReviewHandler = createReviewHandler;
        _acknowledgeReviewHandler = acknowledgeReviewHandler;
        _createGoalHandler = createGoalHandler;
        _updateGoalProgressHandler = updateGoalProgressHandler;
        _goalRepository = goalRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get performance reviews for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of performance reviews</returns>
    [HttpGet("employees/{employeeId:guid}/performance-reviews")]
    [ProducesResponseType(typeof(List<PerformanceReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPerformanceReviews(Guid employeeId, CancellationToken cancellationToken)
    {
        // Employees can view their own reviews; HR/Admin/Managers can view their team
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Admin) &&
            !_currentUserService.IsInRole(Roles.Manager) &&
            _currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to view performance reviews for {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        var query = new GetPerformanceReviewsQuery(employeeId);
        var result = await _getReviewsHandler.HandleAsync(query, cancellationToken);

        return Ok(result.PerformanceReviews);
    }

    /// <summary>
    /// Create a new performance review (Manager or HR Specialist only)
    /// </summary>
    /// <param name="employeeId">Employee ID to review</param>
    /// <param name="reviewDto">Performance review details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created performance review ID</returns>
    [HttpPost("employees/{employeeId:guid}/performance-reviews")]
    [Authorize(Policy = Policies.RequireHROrManager)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePerformanceReview(
        Guid employeeId,
        [FromBody] CreatePerformanceReviewDto reviewDto,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.EmployeeId.HasValue)
        {
            return BadRequest(new { message = "Employee ID not found in token" });
        }

        var command = new CreatePerformanceReviewCommand(
            employeeId,
            _currentUserService.EmployeeId.Value,
            reviewDto.ReviewCycle,
            reviewDto.ReviewPeriodStart,
            reviewDto.ReviewPeriodEnd,
            reviewDto.SelfAssessment);

        var result = await _createReviewHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Performance review {ReviewId} created for employee {EmployeeId} by {ReviewerId}",
            result.PerformanceReviewId, employeeId, _currentUserService.EmployeeId.Value);

        return CreatedAtAction(
            nameof(GetPerformanceReviews),
            new { employeeId },
            new { id = result.PerformanceReviewId, message = "Performance review created successfully" });
    }

    /// <summary>
    /// Acknowledge a performance review (Employee only - acknowledges their own review)
    /// </summary>
    /// <param name="reviewId">Performance review ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPut("performance-reviews/{reviewId:guid}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AcknowledgePerformanceReview(
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.EmployeeId.HasValue)
        {
            return BadRequest(new { message = "Employee ID not found in token" });
        }

        var command = new AcknowledgePerformanceReviewCommand(
            reviewId,
            _currentUserService.EmployeeId.Value);

        var result = await _acknowledgeReviewHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Performance review {ReviewId} acknowledged by employee {EmployeeId}",
            reviewId, _currentUserService.EmployeeId.Value);

        return Ok(new { message = "Performance review acknowledged successfully" });
    }

    /// <summary>
    /// Create a new goal for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="goalDto">Goal details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created goal ID</returns>
    [HttpPost("employees/{employeeId:guid}/goals")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateGoal(
        Guid employeeId,
        [FromBody] CreateGoalDto goalDto,
        CancellationToken cancellationToken)
    {
        // Employees can create goals for themselves; Managers can create for their team; HR can create for anyone
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Manager) &&
            _currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to create goal for {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        var command = new CreateGoalCommand(
            employeeId,
            goalDto.Description,
            goalDto.SuccessCriteria,
            goalDto.TargetDate,
            goalDto.PerformanceReviewId);

        var result = await _createGoalHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Goal {GoalId} created for employee {EmployeeId}",
            result.GoalId, employeeId);

        return CreatedAtAction(
            nameof(GetGoals),
            new { employeeId },
            new { id = result.GoalId, message = "Goal created successfully" });
    }

    /// <summary>
    /// Update goal progress
    /// </summary>
    /// <param name="goalId">Goal ID</param>
    /// <param name="updateDto">Progress update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPut("goals/{goalId:guid}/progress")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateGoalProgress(
        Guid goalId,
        [FromBody] UpdateGoalProgressDto updateDto,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.EmployeeId.HasValue)
        {
            return BadRequest(new { message = "Employee ID not found in token" });
        }

        var command = new UpdateGoalProgressCommand(
            goalId,
            _currentUserService.EmployeeId.Value,
            updateDto.CompletionStatus,
            updateDto.ProgressUpdate);

        var result = await _updateGoalProgressHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Goal {GoalId} progress updated by employee {EmployeeId}",
            goalId, _currentUserService.EmployeeId.Value);

        return Ok(new { message = "Goal progress updated successfully" });
    }

    /// <summary>
    /// Get goals for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of goals</returns>
    [HttpGet("employees/{employeeId:guid}/goals")]
    [ProducesResponseType(typeof(List<GoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGoals(Guid employeeId, CancellationToken cancellationToken)
    {
        // Employees can view their own goals; HR/Admin/Managers can view their team
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Admin) &&
            !_currentUserService.IsInRole(Roles.Manager) &&
            _currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to view goals for {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        var goals = await _goalRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);

        var goalDtos = goals.Select(g => new GoalDto
        {
            Id = g.Id,
            EmployeeId = g.EmployeeId,
            EmployeeName = g.Employee?.FullName ?? string.Empty,
            PerformanceReviewId = g.PerformanceReviewId,
            Description = g.Description,
            SuccessCriteria = g.SuccessCriteria,
            TargetDate = g.TargetDate,
            CompletionStatus = g.CompletionStatus.ToString(),
            ProgressUpdates = g.ProgressUpdates,
            CompletedDate = g.CompletedDate,
            CreatedDate = g.CreatedDate,
            ModifiedDate = g.ModifiedDate
        }).ToList();

        return Ok(goalDtos);
    }
}

/// <summary>
/// DTO for creating a performance review
/// </summary>
public record CreatePerformanceReviewDto(
    ReviewCycle ReviewCycle,
    DateTime ReviewPeriodStart,
    DateTime ReviewPeriodEnd,
    string? SelfAssessment = null);

/// <summary>
/// DTO for creating a goal
/// </summary>
public record CreateGoalDto(
    string Description,
    string? SuccessCriteria,
    DateTime TargetDate,
    Guid? PerformanceReviewId = null);

/// <summary>
/// DTO for updating goal progress
/// </summary>
public record UpdateGoalProgressDto(
    GoalStatus CompletionStatus,
    string? ProgressUpdate = null);

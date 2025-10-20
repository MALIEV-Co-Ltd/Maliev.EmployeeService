using Asp.Versioning;
using FluentValidation;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Leave management controller (User Story 2)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/leave")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly GetLeaveBalancesQueryHandler _getBalancesHandler;
    private readonly GetLeaveRequestsQueryHandler _getRequestsHandler;
    private readonly GetPendingApprovalsQueryHandler _getPendingApprovalsHandler;
    private readonly SubmitLeaveRequestCommandHandler _submitRequestHandler;
    private readonly ApproveRejectLeaveCommandHandler _approveRejectHandler;
    private readonly CancelLeaveRequestCommandHandler _cancelRequestHandler;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<SubmitLeaveRequestDto> _submitValidator;
    private readonly IValidator<ApproveRejectLeaveDto> _approveRejectValidator;
    private readonly IValidator<CancelLeaveRequestDto> _cancelValidator;
    private readonly ILogger<LeaveController> _logger;

    public LeaveController(
        GetLeaveBalancesQueryHandler getBalancesHandler,
        GetLeaveRequestsQueryHandler getRequestsHandler,
        GetPendingApprovalsQueryHandler getPendingApprovalsHandler,
        SubmitLeaveRequestCommandHandler submitRequestHandler,
        ApproveRejectLeaveCommandHandler approveRejectHandler,
        CancelLeaveRequestCommandHandler cancelRequestHandler,
        ICurrentUserService currentUserService,
        IValidator<SubmitLeaveRequestDto> submitValidator,
        IValidator<ApproveRejectLeaveDto> approveRejectValidator,
        IValidator<CancelLeaveRequestDto> cancelValidator,
        ILogger<LeaveController> logger)
    {
        _getBalancesHandler = getBalancesHandler;
        _getRequestsHandler = getRequestsHandler;
        _getPendingApprovalsHandler = getPendingApprovalsHandler;
        _submitRequestHandler = submitRequestHandler;
        _approveRejectHandler = approveRejectHandler;
        _cancelRequestHandler = cancelRequestHandler;
        _currentUserService = currentUserService;
        _submitValidator = submitValidator;
        _approveRejectValidator = approveRejectValidator;
        _cancelValidator = cancelValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get leave balances for an employee including accrued, used, and remaining balances by leave type
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee</param>
    /// <param name="year">Optional year filter (defaults to current year if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of leave balances by type (Annual, Sick, Personal, etc.)</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /v1/leave/balances/3fa85f64-5717-4562-b3fc-2c963f66afa6?year=2025
    ///     Authorization: Bearer {your-jwt-token}
    ///
    /// Authorization:
    /// - Employees can only view their own leave balances
    /// - HR and Admin roles can view any employee's balances
    /// </remarks>
    /// <response code="200">Returns list of leave balances by type</response>
    /// <response code="403">User is not authorized to view these leave balances</response>
    [HttpGet("balances/{employeeId:guid}")]
    [ProducesResponseType(typeof(List<LeaveBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLeaveBalances(Guid employeeId, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        // Employees can only view their own balances; HR/Admin can view any
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Admin) &&
            _currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to view leave balances for {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        var query = new GetLeaveBalancesQuery(employeeId, year);
        var result = await _getBalancesHandler.HandleAsync(query, cancellationToken);

        return Ok(result.Balances);
    }

    /// <summary>
    /// Get leave requests for an employee
    /// </summary>
    [HttpGet("requests/{employeeId:guid}")]
    [ProducesResponseType(typeof(List<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLeaveRequests(Guid employeeId, CancellationToken cancellationToken)
    {
        // Employees can only view their own requests; HR/Admin/Managers can view their team
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Admin) &&
            !_currentUserService.IsInRole(Roles.Manager) &&
            _currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to view leave requests for {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        var query = new GetLeaveRequestsQuery(employeeId);
        var result = await _getRequestsHandler.HandleAsync(query, cancellationToken);

        return Ok(result.LeaveRequests);
    }

    /// <summary>
    /// Get pending leave approvals for current user (manager/approver)
    /// </summary>
    [HttpGet("pending-approvals")]
    [Authorize(Policy = Policies.RequireHROrManager)]
    [ProducesResponseType(typeof(List<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApprovals(CancellationToken cancellationToken)
    {
        if (!_currentUserService.EmployeeId.HasValue)
        {
            return BadRequest(new { message = "Employee ID not found in token" });
        }

        var query = new GetPendingApprovalsQuery(_currentUserService.EmployeeId.Value);
        var result = await _getPendingApprovalsHandler.HandleAsync(query, cancellationToken);

        return Ok(result.PendingApprovals);
    }

    /// <summary>
    /// Submit a new leave request for approval
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee submitting the request</param>
    /// <param name="submitDto">Leave request details including dates, type, and reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created leave request with ID</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /v1/leave/requests/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     Content-Type: application/json
    ///     Authorization: Bearer {your-jwt-token}
    ///
    ///     {
    ///       "leaveType": "Annual",
    ///       "startDate": "2025-11-01T00:00:00Z",
    ///       "endDate": "2025-11-05T00:00:00Z",
    ///       "reason": "Family vacation",
    ///       "isHalfDay": false
    ///     }
    ///
    /// Leave Types: Annual, Sick, Personal, Maternity, Paternity, Unpaid
    ///
    /// Validation Rules:
    /// - Start date must be in the future
    /// - End date must be after or equal to start date
    /// - Employee must have sufficient leave balance
    /// - No overlapping leave requests
    ///
    /// Authorization:
    /// - Employees can only submit leave requests for themselves
    /// </remarks>
    /// <response code="201">Leave request submitted successfully and pending approval</response>
    /// <response code="400">Invalid data, insufficient balance, or overlapping requests</response>
    /// <response code="403">User is not authorized to submit leave request for this employee</response>
    [HttpPost("requests/{employeeId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitLeaveRequest(
        Guid employeeId,
        [FromBody] SubmitLeaveRequestDto submitDto,
        CancellationToken cancellationToken)
    {
        // Employees can only submit for themselves
        if (_currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to submit leave request for {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        // Validate
        var validationResult = await _submitValidator.ValidateAsync(submitDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var command = new SubmitLeaveRequestCommand(employeeId, submitDto);
        var result = await _submitRequestHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Leave request {LeaveRequestId} submitted by employee {EmployeeId}",
            result.LeaveRequestId, employeeId);

        return CreatedAtAction(
            nameof(GetLeaveRequests),
            new { employeeId },
            new { id = result.LeaveRequestId, message = "Leave request submitted successfully" });
    }

    /// <summary>
    /// Approve or reject a pending leave request (Manager/HR only)
    /// </summary>
    /// <param name="leaveRequestId">The unique identifier of the leave request</param>
    /// <param name="decisionDto">Approval decision with comments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message with approval decision</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /v1/leave/requests/1a2b3c4d-5e6f-7890-abcd-ef1234567890/decision
    ///     Content-Type: application/json
    ///     Authorization: Bearer {your-jwt-token}
    ///
    ///     {
    ///       "isApproved": true,
    ///       "comments": "Approved. Have a great vacation!"
    ///     }
    ///
    /// Authorization:
    /// - Requires Manager or HR role
    /// - Managers can approve requests for their direct reports
    /// - HR can approve any leave request
    /// </remarks>
    /// <response code="200">Leave request approved or rejected successfully</response>
    /// <response code="400">Invalid data or leave request not in pending status</response>
    /// <response code="403">User is not authorized to approve/reject this request</response>
    [HttpPut("requests/{leaveRequestId:guid}/decision")]
    [Authorize(Policy = Policies.RequireHROrManager)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveRejectLeaveRequest(
        Guid leaveRequestId,
        [FromBody] ApproveRejectLeaveDto decisionDto,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.EmployeeId.HasValue)
        {
            return BadRequest(new { message = "Employee ID not found in token" });
        }

        // Validate
        var validationResult = await _approveRejectValidator.ValidateAsync(decisionDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var command = new ApproveRejectLeaveCommand(
            leaveRequestId,
            _currentUserService.EmployeeId.Value,
            decisionDto);

        var result = await _approveRejectHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        var action = decisionDto.IsApproved ? "approved" : "rejected";
        _logger.LogInformation("Leave request {LeaveRequestId} {Action} by {ApproverId}",
            leaveRequestId, action, _currentUserService.EmployeeId.Value);

        return Ok(new { message = $"Leave request {action} successfully" });
    }

    /// <summary>
    /// Cancel a leave request
    /// </summary>
    [HttpPut("requests/{leaveRequestId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelLeaveRequest(
        Guid leaveRequestId,
        [FromBody] CancelLeaveRequestDto cancelDto,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.EmployeeId.HasValue)
        {
            return BadRequest(new { message = "Employee ID not found in token" });
        }

        // Validate
        var validationResult = await _cancelValidator.ValidateAsync(cancelDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var command = new CancelLeaveRequestCommand(
            leaveRequestId,
            _currentUserService.EmployeeId.Value,
            cancelDto);

        var result = await _cancelRequestHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Leave request {LeaveRequestId} cancelled by employee {EmployeeId}",
            leaveRequestId, _currentUserService.EmployeeId.Value);

        return Ok(new { message = "Leave request cancelled successfully" });
    }
}

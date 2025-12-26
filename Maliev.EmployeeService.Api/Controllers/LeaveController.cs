using Asp.Versioning;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.Aspire.ServiceDefaults.Authorization;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Leave management controller (User Story 2)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/leave")]
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
    private readonly ILogger<LeaveController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaveController"/> class
    /// </summary>
    public LeaveController(
        GetLeaveBalancesQueryHandler getBalancesHandler,
        GetLeaveRequestsQueryHandler getRequestsHandler,
        GetPendingApprovalsQueryHandler getPendingApprovalsHandler,
        SubmitLeaveRequestCommandHandler submitRequestHandler,
        ApproveRejectLeaveCommandHandler approveRejectHandler,
        CancelLeaveRequestCommandHandler cancelRequestHandler,
        ICurrentUserService currentUserService,
        ILogger<LeaveController> logger)
    {
        _getBalancesHandler = getBalancesHandler;
        _getRequestsHandler = getRequestsHandler;
        _getPendingApprovalsHandler = getPendingApprovalsHandler;
        _submitRequestHandler = submitRequestHandler;
        _approveRejectHandler = approveRejectHandler;
        _cancelRequestHandler = cancelRequestHandler;
        _currentUserService = currentUserService;
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
    [RequirePermission(EmployeePermissions.LeaveRead, ResourcePathTemplate = "employee/{employeeId}/leave")]
    [ProducesResponseType(typeof(List<LeaveBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLeaveBalances(Guid employeeId, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var query = new GetLeaveBalancesQuery(employeeId, year);
        var result = await _getBalancesHandler.HandleAsync(query, cancellationToken);

        return Ok(result.Balances);
    }

    /// <summary>
    /// Get leave requests for an employee
    /// </summary>
    [HttpGet("requests/{employeeId:guid}")]
    [RequirePermission(EmployeePermissions.LeaveRead, ResourcePathTemplate = "employee/{employeeId}/leave")]
    [ProducesResponseType(typeof(List<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLeaveRequests(Guid employeeId, CancellationToken cancellationToken)
    {
        var query = new GetLeaveRequestsQuery(employeeId);
        var result = await _getRequestsHandler.HandleAsync(query, cancellationToken);

        return Ok(result.LeaveRequests);
    }

    /// <summary>
    /// Get pending leave approvals for current user (manager/approver)
    /// </summary>
    [HttpGet("pending-approvals")]
    [RequirePermission(EmployeePermissions.LeaveApprove)]
    [ProducesResponseType(typeof(List<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApprovals(CancellationToken cancellationToken)
    {
        var employeeId = await _currentUserService.GetEmployeeIdAsync(cancellationToken);
        if (employeeId == null)
        {
            return BadRequest(new { message = "Employee profile not found" });
        }

        var query = new GetPendingApprovalsQuery(employeeId.Value);
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
    [RequirePermission(EmployeePermissions.LeaveCreate, ResourcePathTemplate = "employee/{employeeId}/leave")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SubmitLeaveRequest(
        Guid employeeId,
        [FromBody] SubmitLeaveRequestDto submitDto,
        CancellationToken cancellationToken)
    {
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
    [RequirePermission(EmployeePermissions.LeaveApprove)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveRejectLeaveRequest(
        Guid leaveRequestId,
        [FromBody] ApproveRejectLeaveDto decisionDto,
        CancellationToken cancellationToken)
    {
        var approverId = await _currentUserService.GetEmployeeIdAsync(cancellationToken);
        if (approverId == null)
        {
            return BadRequest(new { message = "Approver employee profile not found" });
        }

        var command = new ApproveRejectLeaveCommand(
            leaveRequestId,
            approverId.Value,
            decisionDto);

        var result = await _approveRejectHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        var action = decisionDto.IsApproved ? "approved" : "rejected";
        _logger.LogInformation("Leave request {LeaveRequestId} {Action} by principal {PrincipalId}",
            leaveRequestId, action, _currentUserService.PrincipalId);

        return Ok(new { message = $"Leave request {action} successfully" });
    }

    /// <summary>
    /// Cancel a leave request
    /// </summary>
    [HttpPut("requests/{leaveRequestId:guid}/cancel")]
    [RequirePermission(EmployeePermissions.LeaveCancel)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelLeaveRequest(
        Guid leaveRequestId,
        [FromBody] CancelLeaveRequestDto cancelDto,
        CancellationToken cancellationToken)
    {
        var employeeId = await _currentUserService.GetEmployeeIdAsync(cancellationToken);
        if (employeeId == null)
        {
            return BadRequest(new { message = "Employee profile not found" });
        }

        var command = new CancelLeaveRequestCommand(
            leaveRequestId,
            employeeId.Value,
            cancelDto);

        var result = await _cancelRequestHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Leave request {LeaveRequestId} cancelled by employee principal {PrincipalId}",
            leaveRequestId, _currentUserService.PrincipalId);

        return Ok(new { message = "Leave request cancelled successfully" });
    }
}

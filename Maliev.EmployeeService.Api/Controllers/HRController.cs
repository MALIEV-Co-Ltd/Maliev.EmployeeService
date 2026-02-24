using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// HR personnel employee lifecycle management (User Story 2)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/hr")]
[RequirePermission(EmployeePermissions.ProfilesCreate)]
public class HRController : ControllerBase
{
    private readonly CreateEmployeeCommandHandler _createEmployeeHandler;
    private readonly TransferDepartmentCommandHandler _transferDepartmentHandler;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HRController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HRController"/> class
    /// </summary>
    public HRController(
        CreateEmployeeCommandHandler createEmployeeHandler,
        TransferDepartmentCommandHandler transferDepartmentHandler,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        ILogger<HRController> logger)
    {
        _createEmployeeHandler = createEmployeeHandler;
        _transferDepartmentHandler = transferDepartmentHandler;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Create a new employee record (HR Specialist, System Admin only)
    /// </summary>
    /// <param name="createDto">Employee creation data</param>
    /// <returns>Created employee ID</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("employees")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] CreateEmployeeDto createDto,
        CancellationToken cancellationToken)
    {
        var command = new CreateEmployeeCommand(createDto);
        var result = await _createEmployeeHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Employee {EmployeeId} created with employee number {EmployeeNumber}",
            result.EmployeeId, createDto.EmployeeNumber);

        return CreatedAtRoute(
            "GetEmployeeProfile",
            new { version = "1.0", employeeId = result.EmployeeId },
            new { id = result.EmployeeId, message = "Employee created successfully" });
    }

    /// <summary>
    /// Transfer an employee to a different department
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="request">Transfer request details</param>
    /// <returns>Success result</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("employees/{employeeId:guid}/transfer-department")]
    [RequirePermission(EmployeePermissions.ProfilesUpdate, ResourcePathTemplate = "employee/{employeeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferDepartment(
        Guid employeeId,
        [FromBody] TransferDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.NewDepartmentId == Guid.Empty)
        {
            return BadRequest(new { message = "New department ID is required" });
        }

        if (request.EffectiveDate == default)
        {
            request = request with { EffectiveDate = DateTime.UtcNow };
        }

        var command = new TransferDepartmentCommand(
            employeeId,
            request.NewDepartmentId,
            request.TransferReason,
            request.EffectiveDate);

        var result = await _transferDepartmentHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Employee {EmployeeId} transferred to department {DepartmentId}",
            employeeId, request.NewDepartmentId);

        return Ok(new { message = "Employee transferred successfully" });
    }

    /// <summary>
    /// Assign a dotted-line manager to an employee for matrix reporting
    /// (User Story 5 - Matrix Organizations)
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="request">Dotted-line manager assignment request</param>
    /// <returns>Success result</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("employees/{employeeId:guid}/dotted-line-manager")]
    [RequirePermission(EmployeePermissions.ProfilesUpdate, ResourcePathTemplate = "employee/{employeeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignDottedLineManager(
        Guid employeeId,
        [FromBody] AssignDottedLineManagerRequest request,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee == null)
        {
            return NotFound(new { message = "Employee not found" });
        }

        // Validate that dotted-line manager exists
        if (request.DottedLineManagerId.HasValue)
        {
            var dottedLineManager = await _employeeRepository.GetByIdAsync(
                request.DottedLineManagerId.Value,
                cancellationToken);

            if (dottedLineManager == null)
            {
                return BadRequest(new { message = "Dotted-line manager not found" });
            }

            // Prevent self-assignment
            if (request.DottedLineManagerId.Value == employeeId)
            {
                return BadRequest(new { message = "Employee cannot be their own dotted-line manager" });
            }
        }

        employee.DottedLineManagerId = request.DottedLineManagerId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Dotted-line manager assigned for employee {EmployeeId}: {DottedLineManagerId}",
            employeeId, request.DottedLineManagerId);

        return Ok(new { message = "Dotted-line manager assigned successfully" });
    }

    /// <summary>
    /// Get all dotted-line reports for a manager
    /// (User Story 5 - Matrix Organizations)
    /// </summary>
    /// <param name="managerId">Manager's employee ID</param>
    /// <returns>List of dotted-line reports</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("employees/{managerId:guid}/dotted-line-reports")]
    [RequirePermission(EmployeePermissions.ProfilesRead, ResourcePathTemplate = "employee/managers/{managerId}")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDottedLineReports(
        Guid managerId,
        CancellationToken cancellationToken)
    {
        var manager = await _employeeRepository.GetByIdAsync(managerId, cancellationToken);
        if (manager == null)
        {
            return NotFound(new { message = "Manager not found" });
        }

        // Get all employees where this manager is the dotted-line manager
        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var dottedLineReports = allEmployees
            .Where(e => e.DottedLineManagerId == managerId)
            .Select(e => new
            {
                employeeId = e.Id,
                employeeNumber = e.EmployeeNumber,
                fullName = e.FullName,
                jobTitle = e.JobTitle,
                departmentId = e.DepartmentId,
                primaryManagerId = e.ManagerId,
                workEmail = e.ContactInformation.WorkEmail
            })
            .ToList();

        return Ok(dottedLineReports);
    }
}

/// <summary>
/// Request model for department transfer
/// </summary>
public record TransferDepartmentRequest(
    Guid NewDepartmentId,
    string? TransferReason,
    DateTime EffectiveDate);

/// <summary>
/// Request model for assigning dotted-line manager
/// </summary>
public record AssignDottedLineManagerRequest(Guid? DottedLineManagerId);

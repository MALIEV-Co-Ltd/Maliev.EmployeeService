using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// General employee management and lookups.
/// Supports operations for retrieving employee profile information by different identifiers.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("employee/v{version:apiVersion}/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly GetEmployeeProfileQueryHandler _getProfileHandler;
    private readonly GetEmployeeByEmailQueryHandler _getByEmailHandler;
    private readonly AutoProvisionEmployeeCommandHandler _autoProvisionHandler;
    private readonly ILogger<EmployeesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeesController"/> class.
    /// </summary>
    /// <param name="employeeRepository">The repository for employee data.</param>
    /// <param name="getProfileHandler">The handler for getting employee profiles.</param>
    /// <param name="getByEmailHandler">The handler for getting employee by email.</param>
    /// <param name="autoProvisionHandler">The handler for auto-provisioning employees.</param>
    /// <param name="logger">The logger instance.</param>
    public EmployeesController(
        IEmployeeRepository employeeRepository,
        GetEmployeeProfileQueryHandler getProfileHandler,
        GetEmployeeByEmailQueryHandler getByEmailHandler,
        AutoProvisionEmployeeCommandHandler autoProvisionHandler,
        ILogger<EmployeesController> logger)
    {
        _employeeRepository = employeeRepository;
        _getProfileHandler = getProfileHandler;
        _getByEmailHandler = getByEmailHandler;
        _autoProvisionHandler = autoProvisionHandler;
        _logger = logger;
    }

    /// <summary>
    /// Get a paginated list of all employees.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of employee summaries.</returns>
    [HttpGet]
    [RequirePermission(EmployeePermissions.ProfilesRead)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var allEmployees = (await _employeeRepository.GetAllAsync(cancellationToken)).ToList();
        var total = allEmployees.Count;
        var paged = allEmployees
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                id = e.Id,
                name = e.FullName,
                email = e.ContactInformation.WorkEmail,
                department = string.Empty,
                title = e.JobTitle ?? string.Empty,
                phone = e.ContactInformation.MobilePhone,
                status = e.EmploymentStatus.ToString(),
                hireDate = (DateTime?)e.StartDate,
                manager = (string?)null
            })
            .ToList();

        return Ok(new
        {
            data = paged,
            meta = new
            {
                currentPage = page,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                totalItems = total,
                totalCount = total,
                pageSize
            }
        });
    }

    /// <summary>
    /// Get employee by ID.
    /// </summary>
    /// <param name="id">The employee ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Employee detail.</returns>
    [HttpGet("{id:guid}")]
    [RequirePermission(EmployeePermissions.ProfilesRead, ResourcePathTemplate = "employee/{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetEmployeeProfileQuery(id);
        var result = await _getProfileHandler.HandleAsync(query, cancellationToken);

        if (result.Profile == null)
        {
            _logger.LogWarning("Employee not found with id {Id}", id);
            return NotFound(new { message = $"Employee not found with id {id}" });
        }

        var p = result.Profile;
        return Ok(new
        {
            id = p.Id,
            firstName = p.FirstName,
            lastName = p.LastName,
            email = p.WorkEmail,
            phone = p.MobilePhone,
            department = p.DepartmentName ?? string.Empty,
            title = p.JobTitle ?? string.Empty,
            status = p.EmploymentStatus,
            managerId = p.ManagerId?.ToString(),
            managerName = p.ManagerName,
            hireDate = (DateTime?)p.StartDate,
            terminationDate = p.TerminationDate,
            workLocation = p.WorkLocation,
            employeeType = p.EmploymentType,
            notes = Array.Empty<object>(),
            teams = Array.Empty<object>(),
            emergencyContacts = p.EmergencyContacts.Select(ec => new
            {
                name = ec.ContactName,
                relationship = ec.Relationship,
                phone = ec.PhoneNumber,
                email = ec.Email
            }).ToList(),
            employmentHistory = Array.Empty<object>(),
            documents = Array.Empty<object>(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get employee profile by IAM Principal ID (US2).
    /// </summary>
    /// <param name="principalId">The IAM Principal ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Employee profile.</returns>
    [HttpGet("by-principal/{principalId:guid}")]
    [RequirePermission(EmployeePermissions.ProfilesRead, ResourcePathTemplate = "employee/principals/{principalId}")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPrincipalId(Guid principalId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByPrincipalIdAsync(principalId, cancellationToken);

        if (employee == null)
        {
            _logger.LogWarning("Employee not found for principal {PrincipalId}", principalId);
            return NotFound(new { message = $"Employee not found for principal {principalId}" });
        }

        var query = new GetEmployeeProfileQuery(employee.Id);
        var result = await _getProfileHandler.HandleAsync(query, cancellationToken);

        return Ok(result.Profile);
    }

    /// <summary>
    /// Get employee by work email address (case-insensitive).
    /// Used by AuthService for Google SSO identity resolution.
    /// Internal service-to-service endpoint - requires service account authentication.
    /// </summary>
    /// <param name="email">The work email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Employee lookup data.</returns>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(typeof(EmployeeLookupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByEmail(string email, CancellationToken cancellationToken)
    {
        // Basic email validation
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            _logger.LogWarning("Invalid email format: {Email}", email);
            return BadRequest(new { message = "Invalid email format" });
        }

        var query = new GetEmployeeByEmailQuery(email);
        var result = await _getByEmailHandler.HandleAsync(query, cancellationToken);

        if (result.Employee == null)
        {
            _logger.LogInformation("Employee not found for email: {Email}", email);
            return NotFound(new { message = $"Employee not found with email: {email}" });
        }

        return Ok(result.Employee);
    }

    /// <summary>
    /// Auto-provisions a new employee account from Google Workspace SSO.
    /// Triggered by AuthService when a verified @maliev.com user logs in via Google SSO.
    /// Internal service-to-service endpoint - requires service account authentication.
    /// </summary>
    /// <param name="command">The auto-provision command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created employee data.</returns>
    [HttpPost("auto-provision")]
    [ProducesResponseType(typeof(AutoProvisionEmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AutoProvision([FromBody] AutoProvisionEmployeeCommand command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate @maliev.com domain again for safety
        if (!command.Email.EndsWith("@maliev.com", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Auto-provision rejected for non-maliev.com email: {Email}", command.Email);
            return BadRequest(new { message = "Only @maliev.com email addresses are allowed" });
        }

        var result = await _autoProvisionHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }
}

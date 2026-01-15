using Asp.Versioning;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.EmployeeService.Domain.Authorization;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// General employee management and lookups.
/// Supports operations for retrieving employee profile information by different identifiers.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/employees")]
[Authorize]
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

using Asp.Versioning;
using Maliev.EmployeeService.Api.Models;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Internal authentication and identity validation (US3)
/// Used by IAM and Auth services during migration
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/auth")]
public class EmployeeAuthController : ControllerBase
{
    private readonly EmployeeDbContext _context;
    private readonly ILogger<EmployeeAuthController> _logger;
    private readonly IPasswordService _passwordService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeAuthController"/> class
    /// </summary>
    public EmployeeAuthController(
        EmployeeDbContext context,
        ILogger<EmployeeAuthController> logger,
        IPasswordService passwordService)
    {
        _context = context;
        _logger = logger;
        _passwordService = passwordService;
    }

    /// <summary>
    /// Validates employee credentials and returns principal identity (US3)
    /// </summary>
    /// <param name="request">Credential request</param>
    /// <returns>Validation result with principal identity</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(CredentialValidationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateCredentials([FromBody] ValidateCredentialsRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.ContactInformation.WorkEmail == request.Username);

        if (employee == null || employee.EmploymentStatus != Domain.Enums.EmploymentStatus.Active)
        {
            _logger.LogWarning("Authentication failed for user {Username}: Employee not found or inactive", request.Username);
            return Ok(new CredentialValidationResponse { IsValid = false });
        }

        // Validate password (password-based authentication is mandatory for this endpoint)
        if (string.IsNullOrEmpty(employee.PasswordHash))
        {
            _logger.LogWarning("Authentication failed for user {Username}: SSO-only account", request.Username);
            return Ok(new CredentialValidationResponse { IsValid = false });
        }

        if (!_passwordService.VerifyPassword(request.Password, employee.PasswordHash))
        {
            _logger.LogWarning("Authentication failed for user {Username}: Invalid password", request.Username);
            return Ok(new CredentialValidationResponse { IsValid = false });
        }

        // Final State (Cleanup Phase complete): PrincipalId is now mandatory and the primary identifier.
        var principalId = employee.PrincipalId;

        return Ok(new CredentialValidationResponse
        {
            IsValid = true,
            PrincipalId = principalId,
            Email = employee.ContactInformation.WorkEmail,
            Name = employee.LegalName.FullName
        });
    }
}

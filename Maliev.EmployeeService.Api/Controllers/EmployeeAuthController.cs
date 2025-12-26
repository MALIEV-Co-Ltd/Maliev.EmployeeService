using Asp.Versioning;
using Maliev.EmployeeService.Api.Models;
using Maliev.EmployeeService.Application.Services;
using Maliev.EmployeeService.Infrastructure.Data;
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
    private readonly BusinessMetricsService _metricsService;
    private readonly ILogger<EmployeeAuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeAuthController"/> class
    /// </summary>
    public EmployeeAuthController(
        EmployeeDbContext context, 
        BusinessMetricsService metricsService,
        ILogger<EmployeeAuthController> logger)
    {
        _context = context;
        _metricsService = metricsService;
        _logger = logger;
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
            .FirstOrDefaultAsync(e => e.ContactInformation.WorkEmail == request.Email);

        if (employee == null || employee.EmploymentStatus != Domain.Enums.EmploymentStatus.Active)
        {
            _logger.LogWarning("Authentication failed for user {Email}: Employee not found or inactive", request.Email);
            _metricsService.RecordCredentialValidation(false);
            return Ok(new CredentialValidationResponse { IsValid = false });
        }

        // Final State (Cleanup Phase complete): PrincipalId is now mandatory and the primary identifier.
        var principalId = employee.PrincipalId;

        _metricsService.RecordCredentialValidation(true);
        return Ok(new CredentialValidationResponse
        {
            IsValid = true,
            PrincipalId = principalId,
            Email = employee.ContactInformation.WorkEmail,
            Name = employee.LegalName.FullName
        });
    }
}

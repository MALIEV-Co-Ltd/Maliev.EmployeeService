using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Domain.Authorization;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Controller for managing user preferences.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("employee/v{version:apiVersion}/preferences")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly EmployeeDbContext _context;
    private readonly ILogger<PreferencesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferencesController"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="logger">Logger instance.</param>
    public PreferencesController(EmployeeDbContext context, ILogger<PreferencesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves preferences for the current user and scope.
    /// </summary>
    /// <param name="scope">Preference scope (e.g., dashboard).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User preference details.</returns>
    [HttpGet("{scope}")]
    [RequirePermission(EmployeePermissions.ProfilesRead)]
    [ProducesResponseType(typeof(UserPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPreferenceResponse>> GetPreference(string scope, CancellationToken cancellationToken)
    {
        var principalId = GetCurrentPrincipalId();
        var pref = await _context.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PrincipalId == principalId && p.Scope == scope, cancellationToken);

        if (pref == null)
            return NotFound();

        return Ok(new UserPreferenceResponse
        {
            PrincipalId = pref.PrincipalId,
            Scope = pref.Scope,
            PreferenceData = pref.PreferenceData,
            UpdatedAt = pref.UpdatedAt
        });
    }

    /// <summary>
    /// Creates or updates preferences for the current user and scope.
    /// </summary>
    /// <param name="scope">Preference scope.</param>
    /// <param name="request">Preference data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated preference.</returns>
    [HttpPut("{scope}")]
    [RequirePermission(EmployeePermissions.ProfilesUpdate)]
    [ProducesResponseType(typeof(UserPreferenceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserPreferenceResponse>> UpsertPreference(
        string scope,
        [FromBody] UpsertPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var principalId = GetCurrentPrincipalId();
        var pref = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.PrincipalId == principalId && p.Scope == scope, cancellationToken);

        if (pref == null)
        {
            pref = new UserPreference
            {
                PrincipalId = principalId,
                Scope = scope,
                PreferenceData = request.PreferenceData,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserPreferences.Add(pref);
        }
        else
        {
            pref.PreferenceData = request.PreferenceData;
            pref.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new UserPreferenceResponse
        {
            PrincipalId = pref.PrincipalId,
            Scope = pref.Scope,
            PreferenceData = pref.PreferenceData,
            UpdatedAt = pref.UpdatedAt
        });
    }

    /// <summary>
    /// Deletes preferences for the current user and scope.
    /// </summary>
    /// <param name="scope">Preference scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{scope}")]
    [RequirePermission(EmployeePermissions.ProfilesUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePreference(string scope, CancellationToken cancellationToken)
    {
        var principalId = GetCurrentPrincipalId();
        var pref = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.PrincipalId == principalId && p.Scope == scope, cancellationToken);

        if (pref != null)
        {
            _context.UserPreferences.Remove(pref);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    private Guid GetCurrentPrincipalId()
    {
        // Extract PrincipalId from claims (sub or custom claim)
        var sub = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var principalId))
            return principalId;

        // Fallback or throw if not found/valid (should correspond to IAM PrincipalId)
        // For development/seeding, might need handling.
        // Assuming secure environment with valid tokens.
        throw new UnauthorizedAccessException("Invalid user principal");
    }
}

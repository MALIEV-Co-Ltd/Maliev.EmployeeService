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
/// Employee self-service profile management (User Story 1).
/// Supports operations for viewing and updating personal profile and emergency contacts.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/profile")]
[Authorize]
public class EmployeeProfileController : ControllerBase
{
    private readonly GetEmployeeProfileQueryHandler _getProfileHandler;
    private readonly UpdateEmployeeProfileCommandHandler _updateProfileHandler;
    private readonly CreateEmergencyContactCommandHandler _createContactHandler;
    private readonly UpdateEmergencyContactCommandHandler _updateContactHandler;
    private readonly DeleteEmergencyContactCommandHandler _deleteContactHandler;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<EmployeeProfileController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeProfileController"/> class.
    /// </summary>
    /// <param name="getProfileHandler">The handler for getting employee profiles.</param>
    /// <param name="updateProfileHandler">The handler for updating employee profiles.</param>
    /// <param name="createContactHandler">The handler for creating emergency contacts.</param>
    /// <param name="updateContactHandler">The handler for updating emergency contacts.</param>
    /// <param name="deleteContactHandler">The handler for deleting emergency contacts.</param>
    /// <param name="currentUserService">The current user service.</param>
    /// <param name="logger">The logger instance.</param>
    public EmployeeProfileController(
        GetEmployeeProfileQueryHandler getProfileHandler,
        UpdateEmployeeProfileCommandHandler updateProfileHandler,
        CreateEmergencyContactCommandHandler createContactHandler,
        UpdateEmergencyContactCommandHandler updateContactHandler,
        DeleteEmergencyContactCommandHandler deleteContactHandler,
        ICurrentUserService currentUserService,
        ILogger<EmployeeProfileController> logger)
    {
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _createContactHandler = createContactHandler;
        _updateContactHandler = updateContactHandler;
        _deleteContactHandler = deleteContactHandler;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get employee profile with contact information and emergency contacts.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Employee profile with personal information and emergency contacts.</returns>
    [HttpGet("{employeeId:guid}/profile", Name = "GetEmployeeProfile")]
    [RequirePermission(EmployeePermissions.ProfilesRead, ResourcePathTemplate = "employee/{employeeId}")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid employeeId, CancellationToken cancellationToken)
    {
        var query = new GetEmployeeProfileQuery(employeeId);
        var result = await _getProfileHandler.HandleAsync(query, cancellationToken);

        if (result.Profile == null)
        {
            return NotFound(new { message = "Employee profile not found" });
        }

        return Ok(result.Profile);
    }

    /// <summary>
    /// Update employee profile (limited fields: personal email, mobile phone, preferred name).
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="updateDto">Profile update data containing allowed fields only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message confirming profile update.</returns>
    [HttpPut("{employeeId:guid}/profile")]
    [RequirePermission(EmployeePermissions.ProfilesUpdate, ResourcePathTemplate = "employee/{employeeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        Guid employeeId,
        [FromBody] UpdateEmployeeProfileDto updateDto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeProfileCommand(employeeId, updateDto);
        var result = await _updateProfileHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Employee {EmployeeId} updated profile", employeeId);
        return Ok(new { message = "Profile updated successfully" });
    }

    /// <summary>
    /// Create a new emergency contact for an employee.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="createDto">Emergency contact information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created emergency contact with ID.</returns>
    [HttpPost("{employeeId:guid}/emergency-contacts")]
    [RequirePermission(EmployeePermissions.ProfilesUpdate, ResourcePathTemplate = "employee/{employeeId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEmergencyContact(
        Guid employeeId,
        [FromBody] CreateEmergencyContactDto createDto,
        CancellationToken cancellationToken)
    {
        var command = new CreateEmergencyContactCommand(employeeId, createDto);
        var result = await _createContactHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Emergency contact {ContactId} created for employee {EmployeeId}",
            result.EmergencyContactId, employeeId);

        return CreatedAtAction(
            nameof(GetProfile),
            new { employeeId },
            new { id = result.EmergencyContactId, message = "Emergency contact created successfully" });
    }

    /// <summary>
    /// Update an existing emergency contact.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="contactId">The unique identifier of the emergency contact.</param>
    /// <param name="updateDto">Updated emergency contact information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message confirming update.</returns>
    [HttpPut("{employeeId:guid}/emergency-contacts/{contactId:guid}")]
    [RequirePermission(EmployeePermissions.ProfilesUpdate, ResourcePathTemplate = "employee/{employeeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmergencyContact(
        Guid employeeId,
        Guid contactId,
        [FromBody] UpdateEmergencyContactDto updateDto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEmergencyContactCommand(contactId, updateDto);
        var result = await _updateContactHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Emergency contact {ContactId} updated for employee {EmployeeId}",
            contactId, employeeId);

        return Ok(new { message = "Emergency contact updated successfully" });
    }

    /// <summary>
    /// Delete an emergency contact.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="contactId">The unique identifier of the emergency contact to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message confirming deletion.</returns>
    [HttpDelete("{employeeId:guid}/emergency-contacts/{contactId:guid}")]
    [RequirePermission(EmployeePermissions.ProfilesUpdate, ResourcePathTemplate = "employee/{employeeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmergencyContact(
        Guid employeeId,
        Guid contactId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteEmergencyContactCommand(contactId);
        var result = await _deleteContactHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Emergency contact {ContactId} deleted for employee {EmployeeId}",
            contactId, employeeId);

        return Ok(new { message = "Emergency contact deleted successfully" });
    }
}
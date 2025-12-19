using Asp.Versioning;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Employee self-service profile management (User Story 1)
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
    /// Initializes a new instance of the <see cref="EmployeeProfileController"/> class
    /// </summary>
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
    /// Get employee profile with contact information and emergency contacts
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Employee profile with personal information and emergency contacts</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /v1/profile/3fa85f64-5717-4562-b3fc-2c963f66afa6/profile
    ///     Authorization: Bearer {your-jwt-token}
    ///
    /// Authorization:
    /// - Employees can only view their own profile
    /// - HR and Admin roles can view any employee profile
    /// </remarks>
    /// <response code="200">Returns the employee profile with emergency contacts</response>
    /// <response code="403">User is not authorized to view this profile</response>
    /// <response code="404">Employee profile not found</response>
    [HttpGet("{employeeId:guid}/profile")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid employeeId, CancellationToken cancellationToken)
    {
        // Authorization: Employees can only view their own profile
        // HR and Admins can view any profile
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Admin) &&
            _currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to access employee profile {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        var query = new GetEmployeeProfileQuery(employeeId);
        var result = await _getProfileHandler.HandleAsync(query, cancellationToken);

        if (result.Profile == null)
        {
            return NotFound(new { message = "Employee profile not found" });
        }

        return Ok(result.Profile);
    }

    /// <summary>
    /// Update employee profile (limited fields: personal email, mobile phone, preferred name)
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee</param>
    /// <param name="updateDto">Profile update data containing allowed fields only</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message confirming profile update</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /v1/profile/3fa85f64-5717-4562-b3fc-2c963f66afa6/profile
    ///     Content-Type: application/json
    ///     Authorization: Bearer {your-jwt-token}
    ///
    ///     {
    ///       "personalEmail": "john.doe@personal.com",
    ///       "mobilePhone": "+66812345678",
    ///       "preferredName": "Johnny"
    ///     }
    ///
    /// Authorization:
    /// - Employees can only update their own profile
    /// - Limited to personal email, mobile phone, and preferred name only
    /// </remarks>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid data provided or validation failed</response>
    /// <response code="403">User is not authorized to update this profile</response>
    /// <response code="404">Employee profile not found</response>
    [HttpPut("{employeeId:guid}/profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        Guid employeeId,
        [FromBody] UpdateEmployeeProfileDto updateDto,
        CancellationToken cancellationToken)
    {
        // Authorization: Employees can only update their own profile
        if (_currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to update employee profile {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

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
    /// Create a new emergency contact for an employee
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee</param>
    /// <param name="createDto">Emergency contact information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created emergency contact with ID</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /v1/profile/3fa85f64-5717-4562-b3fc-2c963f66afa6/emergency-contacts
    ///     Content-Type: application/json
    ///     Authorization: Bearer {your-jwt-token}
    ///
    ///     {
    ///       "fullName": "Jane Doe",
    ///       "relationship": "Spouse",
    ///       "phoneNumber": "+66812345678",
    ///       "email": "jane.doe@email.com",
    ///       "isPrimary": true
    ///     }
    ///
    /// Authorization:
    /// - Employees can only create emergency contacts for themselves
    /// </remarks>
    /// <response code="201">Emergency contact created successfully</response>
    /// <response code="400">Invalid data provided or validation failed</response>
    /// <response code="403">User is not authorized to create emergency contact for this employee</response>
    [HttpPost("{employeeId:guid}/emergency-contacts")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEmergencyContact(
        Guid employeeId,
        [FromBody] CreateEmergencyContactDto createDto,
        CancellationToken cancellationToken)
    {
        // Authorization: Employees can only create contacts for themselves
        if (_currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to create emergency contact for employee {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

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
    /// Update an existing emergency contact
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee</param>
    /// <param name="contactId">The unique identifier of the emergency contact</param>
    /// <param name="updateDto">Updated emergency contact information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message confirming update</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /v1/profile/3fa85f64-5717-4562-b3fc-2c963f66afa6/emergency-contacts/1a2b3c4d-5e6f-7890-abcd-ef1234567890
    ///     Content-Type: application/json
    ///     Authorization: Bearer {your-jwt-token}
    ///
    ///     {
    ///       "fullName": "Jane Doe",
    ///       "relationship": "Spouse",
    ///       "phoneNumber": "+66898765432",
    ///       "email": "jane.doe.updated@email.com",
    ///       "isPrimary": true
    ///     }
    ///
    /// Authorization:
    /// - Employees can only update their own emergency contacts
    /// </remarks>
    /// <response code="200">Emergency contact updated successfully</response>
    /// <response code="400">Invalid data provided or validation failed</response>
    /// <response code="403">User is not authorized to update this emergency contact</response>
    /// <response code="404">Emergency contact not found</response>
    [HttpPut("{employeeId:guid}/emergency-contacts/{contactId:guid}")]
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
        // Authorization: Employees can only update their own contacts
        if (_currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to update emergency contact for employee {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

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
    /// Delete an emergency contact
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee</param>
    /// <param name="contactId">The unique identifier of the emergency contact to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message confirming deletion</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     DELETE /v1/profile/3fa85f64-5717-4562-b3fc-2c963f66afa6/emergency-contacts/1a2b3c4d-5e6f-7890-abcd-ef1234567890
    ///     Authorization: Bearer {your-jwt-token}
    ///
    /// Authorization:
    /// - Employees can only delete their own emergency contacts
    /// </remarks>
    /// <response code="200">Emergency contact deleted successfully</response>
    /// <response code="403">User is not authorized to delete this emergency contact</response>
    /// <response code="404">Emergency contact not found</response>
    [HttpDelete("{employeeId:guid}/emergency-contacts/{contactId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmergencyContact(
        Guid employeeId,
        Guid contactId,
        CancellationToken cancellationToken)
    {
        // Authorization: Employees can only delete their own contacts
        if (_currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to delete emergency contact for employee {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

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

using Asp.Versioning;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Training and Certification Management controller (User Story 8)
/// Manages training records, certifications, and skills matrix
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("employee/v{version:apiVersion}/training")]
[Authorize]
public class TrainingController : ControllerBase
{
    private readonly GetTrainingRecordsQueryHandler _getTrainingRecordsHandler;
    private readonly GetTrainingComplianceReportQueryHandler _getComplianceReportHandler;
    private readonly RecordTrainingCompletionCommandHandler _recordTrainingHandler;
    private readonly UpdateSkillCommandHandler _updateSkillHandler;
    private readonly ISkillRepository _skillRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TrainingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainingController"/> class
    /// </summary>
    public TrainingController(
        GetTrainingRecordsQueryHandler getTrainingRecordsHandler,
        GetTrainingComplianceReportQueryHandler getComplianceReportHandler,
        RecordTrainingCompletionCommandHandler recordTrainingHandler,
        UpdateSkillCommandHandler updateSkillHandler,
        ISkillRepository skillRepository,
        ICurrentUserService currentUserService,
        ILogger<TrainingController> logger)
    {
        _getTrainingRecordsHandler = getTrainingRecordsHandler;
        _getComplianceReportHandler = getComplianceReportHandler;
        _recordTrainingHandler = recordTrainingHandler;
        _updateSkillHandler = updateSkillHandler;
        _skillRepository = skillRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get training records for an employee (T279)
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="trainingType">Optional filter by training type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of training records</returns>
    [HttpGet("employees/{employeeId:guid}/training-records")]
    [ProducesResponseType(typeof(List<TrainingRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTrainingRecords(
        Guid employeeId,
        [FromQuery] TrainingType? trainingType,
        CancellationToken cancellationToken)
    {
        // Employees can view their own records; HR/Admin/Managers can view their team
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Admin) &&
            !_currentUserService.IsInRole(Roles.Manager) &&
            _currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to view training records for {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        var query = new GetTrainingRecordsQuery
        {
            EmployeeId = employeeId,
            FilterByType = trainingType
        };

        var result = await _getTrainingRecordsHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Record training completion for an employee (T280)
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="trainingDto">Training record details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created training record</returns>
    [HttpPost("employees/{employeeId:guid}/training-records")]
    [Authorize(Policy = Policies.RequireHROrManager)]
    [ProducesResponseType(typeof(TrainingRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordTrainingCompletion(
        Guid employeeId,
        [FromBody] CreateTrainingRecordDto trainingDto,
        CancellationToken cancellationToken)
    {
        var command = new RecordTrainingCompletionCommand
        {
            EmployeeId = employeeId,
            CourseName = trainingDto.CourseName,
            CompletionDate = trainingDto.CompletionDate,
            ExpirationDate = trainingDto.ExpirationDate,
            CertificateDocumentId = trainingDto.CertificateDocumentId,
            TrainingType = trainingDto.TrainingType,
            Provider = trainingDto.Provider
        };

        var trainingRecord = await _recordTrainingHandler.HandleAsync(command, cancellationToken);

        var responseDto = new TrainingRecordDto
        {
            Id = trainingRecord.Id,
            CourseName = trainingRecord.CourseName,
            CompletionDate = trainingRecord.CompletionDate,
            ExpirationDate = trainingRecord.ExpirationDate,
            CertificateDocumentId = trainingRecord.CertificateDocumentId,
            TrainingType = trainingRecord.TrainingType,
            Provider = trainingRecord.Provider,
            Status = trainingRecord.Status,
            DaysUntilExpiration = trainingRecord.ExpirationDate.HasValue
                ? (int)(trainingRecord.ExpirationDate.Value - DateTime.UtcNow).TotalDays
                : null
        };

        _logger.LogInformation("Training record {TrainingId} created for employee {EmployeeId}",
            trainingRecord.Id, employeeId);

        return CreatedAtAction(
            nameof(GetTrainingRecords),
            new { employeeId },
            responseDto);
    }

    /// <summary>
    /// Get training compliance report (T281) - HR only
    /// </summary>
    /// <param name="departmentId">Optional filter by department</param>
    /// <param name="trainingType">Optional filter by training type</param>
    /// <param name="onlyOverdue">Show only employees with overdue training</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training compliance report</returns>
    [HttpGet("compliance-report")]
    [Authorize(Policy = Policies.RequireHRRole)]
    [ProducesResponseType(typeof(TrainingComplianceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTrainingComplianceReport(
        [FromQuery] Guid? departmentId,
        [FromQuery] TrainingType? trainingType,
        [FromQuery] bool onlyOverdue = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTrainingComplianceReportQuery
        {
            DepartmentId = departmentId,
            TrainingType = trainingType,
            OnlyOverdue = onlyOverdue
        };

        var report = await _getComplianceReportHandler.HandleAsync(query, cancellationToken);

        _logger.LogInformation("Training compliance report generated: {TotalEmployees} employees, {ComplianceRate}% compliant",
            report.TotalEmployees, report.ComplianceRate);

        return Ok(report);
    }

    /// <summary>
    /// Create a skill record for an employee (T282)
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="skillDto">Skill details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created skill record</returns>
    [HttpPost("employees/{employeeId:guid}/skills")]
    [ProducesResponseType(typeof(EmployeeSkillDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSkill(
        Guid employeeId,
        [FromBody] CreateSkillDto skillDto,
        CancellationToken cancellationToken)
    {
        // Employees can create skills for themselves; Managers can create for their team; HR can create for anyone
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Manager) &&
            _currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to create skill for {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            SkillName = skillDto.SkillName,
            ProficiencyLevel = skillDto.ProficiencyLevel,
            LastAssessedDate = DateTime.UtcNow,
            IsDevelopmentArea = skillDto.IsDevelopmentArea,
            Notes = skillDto.Notes,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var createdSkill = await _skillRepository.CreateAsync(skill, cancellationToken);

        var responseDto = new EmployeeSkillDto
        {
            Id = createdSkill.Id,
            EmployeeId = createdSkill.EmployeeId,
            SkillName = createdSkill.SkillName,
            ProficiencyLevel = createdSkill.ProficiencyLevel,
            LastAssessedDate = createdSkill.LastAssessedDate,
            IsDevelopmentArea = createdSkill.IsDevelopmentArea,
            Notes = createdSkill.Notes
        };

        _logger.LogInformation("Skill {SkillId} created for employee {EmployeeId}",
            createdSkill.Id, employeeId);

        return CreatedAtAction(
            nameof(GetSkills),
            new { employeeId },
            responseDto);
    }

    /// <summary>
    /// Update a skill record (T283)
    /// </summary>
    /// <param name="skillId">Skill ID</param>
    /// <param name="updateDto">Updated skill details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPut("skills/{skillId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSkill(
        Guid skillId,
        [FromBody] UpdateSkillDto updateDto,
        CancellationToken cancellationToken)
    {
        // Check authorization - employees can update their own skills; HR/Managers can update anyone's
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Manager))
        {
            // Check if the skill belongs to the current user
            var existingSkill = await _skillRepository.GetByIdAsync(skillId, cancellationToken);
            if (existingSkill == null)
            {
                return NotFound(new { message = "Skill not found" });
            }

            if (existingSkill.EmployeeId != _currentUserService.EmployeeId)
            {
                _logger.LogWarning("User {EmployeeId} attempted to update skill {SkillId} belonging to {OwnerId}",
                    _currentUserService.EmployeeId, skillId, existingSkill.EmployeeId);
                return Forbid();
            }
        }

        try
        {
            var command = new UpdateSkillCommand
            {
                SkillId = skillId,
                ProficiencyLevel = updateDto.ProficiencyLevel,
                IsDevelopmentArea = updateDto.IsDevelopmentArea,
                Notes = updateDto.Notes
            };

            var updatedSkill = await _updateSkillHandler.HandleAsync(command, cancellationToken);

            _logger.LogInformation("Skill {SkillId} updated by user {UserId}",
                skillId, _currentUserService.EmployeeId);

            return Ok(new { message = "Skill updated successfully" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get skills for an employee (T284)
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of skills</returns>
    [HttpGet("employees/{employeeId:guid}/skills")]
    [ProducesResponseType(typeof(List<EmployeeSkillDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSkills(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        // Employees can view their own skills; HR/Admin/Managers can view their team
        if (!_currentUserService.IsInRole(Roles.HR) &&
            !_currentUserService.IsInRole(Roles.Admin) &&
            !_currentUserService.IsInRole(Roles.Manager) &&
            _currentUserService.EmployeeId != employeeId)
        {
            _logger.LogWarning("User {EmployeeId} attempted to view skills for {TargetEmployeeId}",
                _currentUserService.EmployeeId, employeeId);
            return Forbid();
        }

        var skills = await _skillRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);

        var skillDtos = skills.Select(s => new EmployeeSkillDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            SkillName = s.SkillName,
            ProficiencyLevel = s.ProficiencyLevel,
            LastAssessedDate = s.LastAssessedDate,
            IsDevelopmentArea = s.IsDevelopmentArea,
            Notes = s.Notes
        }).ToList();

        return Ok(skillDtos);
    }
}

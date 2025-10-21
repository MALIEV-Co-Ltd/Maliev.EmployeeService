using Asp.Versioning;
using FluentValidation;
using Maliev.EmployeeService.Api.Authorization;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.EmployeeService.Api.Controllers;

/// <summary>
/// Department and organizational structure management (User Story 2)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly CreateDepartmentCommandHandler _createDepartmentHandler;
    private readonly UpdateDepartmentCommandHandler _updateDepartmentHandler;
    private readonly IValidator<CreateDepartmentDto> _createDepartmentValidator;
    private readonly IValidator<UpdateDepartmentCommand> _updateDepartmentValidator;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(
        IDepartmentRepository departmentRepository,
        CreateDepartmentCommandHandler createDepartmentHandler,
        UpdateDepartmentCommandHandler updateDepartmentHandler,
        IValidator<CreateDepartmentDto> createDepartmentValidator,
        IValidator<UpdateDepartmentCommand> updateDepartmentValidator,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<DepartmentsController> logger)
    {
        _departmentRepository = departmentRepository;
        _createDepartmentHandler = createDepartmentHandler;
        _updateDepartmentHandler = updateDepartmentHandler;
        _createDepartmentValidator = createDepartmentValidator;
        _updateDepartmentValidator = updateDepartmentValidator;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all departments with hierarchical structure
    /// </summary>
    /// <returns>List of departments with parent/child relationships</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDepartments(CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetAllWithSubDepartmentsAsync(cancellationToken);

        var departmentDtos = departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            ParentDepartmentId = d.ParentDepartmentId,
            ParentDepartmentName = d.ParentDepartment?.Name,
            DepartmentHeadId = d.DepartmentHeadId,
            DepartmentHeadName = d.DepartmentHead?.FullName,
            CostCenter = d.CostCenter,
            HeadcountLimit = d.HeadcountLimit,
            CurrentHeadcount = d.CurrentHeadcount,
            IsActive = d.IsActive,
            IsRootDepartment = d.IsRootDepartment,
            HasSubDepartments = d.HasSubDepartments,
            IsAtHeadcountLimit = d.IsAtHeadcountLimit,
            IsApproachingHeadcountLimit = d.IsApproachingHeadcountLimit
        }).ToList();

        return Ok(departmentDtos);
    }

    /// <summary>
    /// Get a specific department by ID with details
    /// </summary>
    /// <param name="departmentId">Department ID</param>
    /// <returns>Department details with employees</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{departmentId:guid}")]
    [ProducesResponseType(typeof(DepartmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartment(Guid departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdWithSubDepartmentsAsync(departmentId, cancellationToken);

        if (department == null)
        {
            return NotFound(new { message = "Department not found" });
        }

        var departmentDto = new DepartmentDetailDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            ParentDepartmentId = department.ParentDepartmentId,
            ParentDepartmentName = department.ParentDepartment?.Name,
            DepartmentHeadId = department.DepartmentHeadId,
            DepartmentHeadName = department.DepartmentHead?.FullName,
            CostCenter = department.CostCenter,
            HeadcountLimit = department.HeadcountLimit,
            CurrentHeadcount = department.CurrentHeadcount,
            IsActive = department.IsActive,
            SubDepartments = department.SubDepartments.Select(sd => new DepartmentDto
            {
                Id = sd.Id,
                Name = sd.Name,
                Description = sd.Description,
                ParentDepartmentId = sd.ParentDepartmentId,
                CurrentHeadcount = sd.CurrentHeadcount,
                IsActive = sd.IsActive
            }).ToList(),
            Employees = department.Employees.Select(e => new EmployeeSummaryDto
            {
                Id = e.Id,
                EmployeeNumber = e.EmployeeNumber,
                FullName = e.FullName,
                JobTitle = e.JobTitle,
                EmploymentStatus = e.EmploymentStatus.ToString()
            }).ToList()
        };

        return Ok(departmentDto);
    }

    /// <summary>
    /// Get department hierarchy (org chart)
    /// </summary>
    /// <returns>Complete hierarchical structure of all departments</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("hierarchy")]
    [ProducesResponseType(typeof(IEnumerable<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartmentHierarchy(CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetHierarchyAsync(cancellationToken);

        var departmentDtos = departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            ParentDepartmentId = d.ParentDepartmentId,
            ParentDepartmentName = d.ParentDepartment?.Name,
            DepartmentHeadId = d.DepartmentHeadId,
            DepartmentHeadName = d.DepartmentHead?.FullName,
            CostCenter = d.CostCenter,
            HeadcountLimit = d.HeadcountLimit,
            CurrentHeadcount = d.CurrentHeadcount,
            IsActive = d.IsActive,
            IsRootDepartment = d.IsRootDepartment,
            HasSubDepartments = d.HasSubDepartments
        }).ToList();

        return Ok(departmentDtos);
    }

    /// <summary>
    /// Get departments approaching or at headcount limit
    /// </summary>
    /// <returns>List of departments needing attention</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("capacity-alerts")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(IEnumerable<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapacityAlerts(CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetDepartmentsNearHeadcountLimitAsync(cancellationToken);

        var departmentDtos = departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            DepartmentHeadName = d.DepartmentHead?.FullName,
            HeadcountLimit = d.HeadcountLimit,
            CurrentHeadcount = d.CurrentHeadcount,
            IsAtHeadcountLimit = d.IsAtHeadcountLimit,
            IsApproachingHeadcountLimit = d.IsApproachingHeadcountLimit
        }).ToList();

        return Ok(departmentDtos);
    }

    /// <summary>
    /// Create a new department (HR Specialist, System Admin only)
    /// </summary>
    /// <param name="createDto">Department creation data</param>
    /// <returns>Created department ID</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateDepartment(
        [FromBody] CreateDepartmentDto createDto,
        CancellationToken cancellationToken)
    {
        // Validate
        var validationResult = await _createDepartmentValidator.ValidateAsync(createDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var command = new CreateDepartmentCommand(createDto);
        var result = await _createDepartmentHandler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Department {DepartmentId} '{DepartmentName}' created",
            result.DepartmentId, createDto.Name);

        return CreatedAtAction(
            nameof(GetDepartment),
            new { version = "1.0", departmentId = result.DepartmentId },
            new { id = result.DepartmentId, message = "Department created successfully" });
    }

    /// <summary>
    /// Get employees in a department
    /// </summary>
    /// <param name="departmentId">Department ID</param>
    /// <param name="includeSubDepartments">Include employees from subdepartments</param>
    /// <returns>List of employees</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{departmentId:guid}/employees")]
    [ProducesResponseType(typeof(IEnumerable<EmployeeSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartmentEmployees(
        Guid departmentId,
        [FromQuery] bool includeSubDepartments = false,
        CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdWithSubDepartmentsAsync(departmentId, cancellationToken);

        if (department == null)
        {
            return NotFound(new { message = "Department not found" });
        }

        var employees = department.Employees.AsEnumerable();

        if (includeSubDepartments)
        {
            var subDepartments = await _departmentRepository.GetAllSubDepartmentsRecursiveAsync(departmentId, cancellationToken);
            var subDepartmentIds = subDepartments.Select(d => d.Id).ToList();

            // Load employees from subdepartments
            var allDepartments = new[] { department }.Concat(subDepartments);
            employees = allDepartments.SelectMany(d => d.Employees);
        }

        var employeeDtos = employees
            .Where(e => e.IsActive)
            .Select(e => new EmployeeSummaryDto
            {
                Id = e.Id,
                EmployeeNumber = e.EmployeeNumber,
                FullName = e.FullName,
                JobTitle = e.JobTitle,
                EmploymentStatus = e.EmploymentStatus.ToString()
            })
            .ToList();

        return Ok(employeeDtos);
    }

    /// <summary>
    /// Update an existing department (HR Specialist, System Admin only)
    /// </summary>
    /// <param name="departmentId">Department ID</param>
    /// <param name="updateCommand">Department update data</param>
    /// <returns>Update result with any warnings</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("{departmentId:guid}")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateDepartment(
        Guid departmentId,
        [FromBody] UpdateDepartmentCommand updateCommand,
        CancellationToken cancellationToken)
    {
        updateCommand.DepartmentId = departmentId;

        // Validate
        var validationResult = await _updateDepartmentValidator.ValidateAsync(updateCommand, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _updateDepartmentHandler.HandleAsync(updateCommand, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Department {DepartmentId} updated", departmentId);

        return Ok(new
        {
            message = "Department updated successfully",
            warnings = result.Warnings
        });
    }

    /// <summary>
    /// Delete (deactivate) a department (HR Specialist, System Admin only)
    /// </summary>
    /// <param name="departmentId">Department ID</param>
    /// <returns>Deletion result</returns>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("{departmentId:guid}")]
    [Authorize(Policy = Policies.RequireHROrAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDepartment(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);

        if (department == null)
        {
            return NotFound(new { message = "Department not found" });
        }

        // Check if department has active employees
        var activeEmployeeCount = await _employeeRepository.GetCountByDepartmentAsync(departmentId, cancellationToken);

        if (activeEmployeeCount > 0)
        {
            return BadRequest(new
            {
                message = $"Cannot delete department with {activeEmployeeCount} active employee(s). Please reassign employees first.",
                activeEmployeeCount
            });
        }

        // Soft delete by marking inactive
        department.IsActive = false;
        department.ModifiedDate = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Department {DepartmentId} '{DepartmentName}' deactivated",
            department.Id, department.Name);

        return Ok(new { message = "Department deactivated successfully" });
    }
}

using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.IntegrationEvents;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to create a new employee record (HR Specialist only)
/// </summary>
public record CreateEmployeeCommand(CreateEmployeeDto EmployeeData);

/// <summary>
/// Response containing the newly created employee ID
/// </summary>
public record CreateEmployeeCommandResult(bool Success, Guid? EmployeeId, string? ErrorMessage);

/// <summary>
/// Handler for CreateEmployeeCommand - enforces business rules and validations
/// </summary>
public class CreateEmployeeCommandHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICareerServiceClient _careerServiceClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventPublisher _eventPublisher;
    private readonly AssignMandatoryTrainingCommandHandler? _assignMandatoryTrainingHandler;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ICareerServiceClient careerServiceClient,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEventPublisher eventPublisher,
        ILogger<CreateEmployeeCommandHandler> logger,
        AssignMandatoryTrainingCommandHandler? assignMandatoryTrainingHandler = null)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _careerServiceClient = careerServiceClient;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _eventPublisher = eventPublisher;
        _assignMandatoryTrainingHandler = assignMandatoryTrainingHandler;
        _logger = logger;
    }

    public async Task<CreateEmployeeCommandResult> HandleAsync(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        var dto = command.EmployeeData;

        // Validate employee number is unique
        var existingEmployee = await _employeeRepository.GetByEmployeeNumberAsync(dto.EmployeeNumber, cancellationToken);
        if (existingEmployee != null)
        {
            return new CreateEmployeeCommandResult(
                false,
                null,
                $"Employee number '{dto.EmployeeNumber}' already exists");
        }

        // Validate department exists if provided
        if (dto.DepartmentId.HasValue)
        {
            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId.Value, cancellationToken);
            if (department == null)
            {
                return new CreateEmployeeCommandResult(
                    false,
                    null,
                    $"Department with ID '{dto.DepartmentId.Value}' not found");
            }

            if (!department.IsActive)
            {
                return new CreateEmployeeCommandResult(
                    false,
                    null,
                    "Cannot assign employee to inactive department");
            }

            // Check headcount limit
            if (department.IsAtHeadcountLimit)
            {
                return new CreateEmployeeCommandResult(
                    false,
                    null,
                    $"Department '{department.Name}' has reached its headcount limit of {department.HeadcountLimit}");
            }
        }

        // Validate manager exists if provided
        if (dto.ManagerId.HasValue)
        {
            var manager = await _employeeRepository.GetByIdAsync(dto.ManagerId.Value, cancellationToken);
            if (manager == null)
            {
                return new CreateEmployeeCommandResult(
                    false,
                    null,
                    $"Manager with ID '{dto.ManagerId.Value}' not found");
            }

            if (!manager.IsActive)
            {
                return new CreateEmployeeCommandResult(
                    false,
                    null,
                    "Cannot assign inactive employee as manager");
            }
        }

        // Validate work location exists in Career Service catalog (FR-164)
        if (dto.WorkLocationId.HasValue)
        {
            try
            {
                var workLocation = await _careerServiceClient.GetWorkLocationByIdAsync(
                    dto.WorkLocationId.Value,
                    cancellationToken);

                if (workLocation == null)
                {
                    return new CreateEmployeeCommandResult(
                        false,
                        null,
                        $"Work location with ID '{dto.WorkLocationId.Value}' not found in Career Service catalog");
                }

                if (!workLocation.IsActive)
                {
                    return new CreateEmployeeCommandResult(
                        false,
                        null,
                        $"Work location '{workLocation.LocationName}' is not active");
                }

                // Optionally set work location name from Career Service
                if (string.IsNullOrEmpty(dto.WorkLocation))
                {
                    dto.WorkLocation = workLocation.LocationName;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the request if Career Service is unavailable
                // Circuit breaker will handle retries and eventual failures
                return new CreateEmployeeCommandResult(
                    false,
                    null,
                    $"Unable to validate work location: {ex.Message}");
            }
        }

        // Validate start date
        if (dto.StartDate > DateTime.UtcNow.AddYears(1))
        {
            return new CreateEmployeeCommandResult(
                false,
                null,
                "Start date cannot be more than 1 year in the future");
        }

        // Validate probation end date if provided
        if (dto.ProbationEndDate.HasValue)
        {
            if (dto.ProbationEndDate.Value <= dto.StartDate)
            {
                return new CreateEmployeeCommandResult(
                    false,
                    null,
                    "Probation end date must be after start date");
            }

            if (dto.ProbationEndDate.Value > dto.StartDate.AddDays(180))
            {
                return new CreateEmployeeCommandResult(
                    false,
                    null,
                    "Probation period cannot exceed 180 days");
            }
        }

        // Create the employee entity
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = dto.EmployeeNumber,
            LegalName = new LegalName
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                MiddleName = dto.MiddleName
            },
            PreferredName = dto.PreferredName,
            DateOfBirth = dto.DateOfBirth,
            Nationality = dto.Nationality,
            ContactInformation = new ContactInformation
            {
                WorkEmail = dto.WorkEmail,
                PersonalEmail = dto.PersonalEmail,
                MobilePhone = dto.MobilePhone
            },
            EmploymentType = dto.EmploymentType,
            EmploymentStatus = EmploymentStatus.Active, // Will be active or pending based on start date
            JobTitle = dto.JobTitle,
            DepartmentId = dto.DepartmentId,
            ManagerId = dto.ManagerId,
            WorkLocation = dto.WorkLocation,
            StartDate = dto.StartDate,
            ProbationEndDate = dto.ProbationEndDate,
            NationalId = dto.NationalId, // Will be encrypted by interceptor
            JobApplicationId = dto.JobApplicationId,
            CreatedBy = _currentUserService.EmployeeId,
            CreatedDate = DateTime.UtcNow
        };

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Auto-assign mandatory training based on employment type and job role (T275 - US8)
        if (_assignMandatoryTrainingHandler != null)
        {
            try
            {
                var assignTrainingCommand = new AssignMandatoryTrainingCommand { EmployeeId = employee.Id };
                var assignedCourses = await _assignMandatoryTrainingHandler.HandleAsync(assignTrainingCommand, cancellationToken);

                if (assignedCourses.Any())
                {
                    _logger.LogInformation(
                        "Auto-assigned {Count} mandatory training courses to new employee {EmployeeId}",
                        assignedCourses.Count,
                        employee.Id);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the request - training assignment is not critical
                _logger.LogWarning(
                    ex,
                    "Failed to auto-assign mandatory training for employee {EmployeeId}",
                    employee.Id);
            }
        }

        // Publish EmployeeCreatedIntegrationEvent (T186 - US10 Integration)
        try
        {
            var department = dto.DepartmentId.HasValue
                ? await _departmentRepository.GetByIdAsync(dto.DepartmentId.Value, cancellationToken)
                : null;

            var integrationEvent = new EmployeeCreatedIntegrationEvent
            {
                EmployeeId = employee.Id,
                EmployeeNumber = employee.EmployeeNumber,
                FullName = $"{employee.LegalName.FirstName} {employee.LegalName.LastName}",
                Email = employee.ContactInformation.WorkEmail,
                StartDate = employee.StartDate,
                Department = department?.Name ?? "Unassigned",
                JobTitle = employee.JobTitle ?? "Employee",
                EventTimestamp = DateTime.UtcNow
            };

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "Published EmployeeCreatedIntegrationEvent for employee {EmployeeId} ({EmployeeNumber})",
                employee.Id,
                employee.EmployeeNumber);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request - event publishing is not critical
            _logger.LogError(
                ex,
                "Failed to publish EmployeeCreatedIntegrationEvent for employee {EmployeeId}",
                employee.Id);
        }

        return new CreateEmployeeCommandResult(true, employee.Id, null);
    }
}

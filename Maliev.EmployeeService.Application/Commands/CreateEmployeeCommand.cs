using Maliev.EmployeeService.Application.DTOs;
// using Maliev.EmployeeService.Domain.IntegrationEvents; // Removed
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Maliev.MessagingContracts.Generated;
using Maliev.MessagingContracts.Contracts.Employee;
using Microsoft.Extensions.Configuration;
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
    private readonly IIAMClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ICareerServiceClient careerServiceClient,
        IIAMClient iamClient,
        IConfiguration configuration,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEventPublisher eventPublisher,
        ILogger<CreateEmployeeCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _careerServiceClient = careerServiceClient;
        _iamClient = iamClient;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _eventPublisher = eventPublisher;
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

        // Create IAM Principal (US1) - Mandatory after migration cleanup
        Guid principalId;
        try
        {
            var principalRequest = new CreatePrincipalRequest
            {
                Email = dto.WorkEmail,
                LinkedService = "EmployeeService",
                LinkedEntityId = null // Will be set after employee creation if needed
            };

            var principalResponse = await _iamClient.CreatePrincipalAsync(principalRequest, cancellationToken);
            principalId = principalResponse.PrincipalId;

            _logger.LogInformation("Created IAM principal {PrincipalId} for employee {EmployeeNumber}", principalId, dto.EmployeeNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create principal in IAM for employee {EmployeeNumber}", dto.EmployeeNumber);
            return new CreateEmployeeCommandResult(
                false,
                null,
                "Failed to create employee identity in IAM. Please ensure identity service is available.");
        }

        // Create the employee entity
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
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
            CreatedBy = _currentUserService.PrincipalId,
            CreatedDate = DateTime.UtcNow
        };

        await _employeeRepository.AddAsync(employee, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Employee {EmployeeId} ({EmployeeNumber}) created successfully", employee.Id, employee.EmployeeNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save employee to database. Attempting to delete orphaned IAM principal {PrincipalId}", principalId);

            // Compensation: Delete the IAM principal to avoid orphaned identity
            await _iamClient.DeletePrincipalAsync(principalId, cancellationToken);

            return new CreateEmployeeCommandResult(
                false,
                null,
                "Failed to create employee due to database error. IAM principal has been cleaned up.");
        }

        // Publish EmployeeCreatedIntegrationEvent (Phase 3 - T125)
        try
        {
            var payload = new EmployeeCreatedEventPayload(
                EmployeeId: employee.Id,
                EmployeeNumber: employee.EmployeeNumber,
                PrincipalId: employee.PrincipalId,
                Email: employee.ContactInformation.WorkEmail,
                FullName: $"{employee.LegalName.FirstName} {employee.LegalName.LastName}",
                StartDate: employee.StartDate,
                DepartmentId: employee.DepartmentId ?? Guid.Empty,
                PositionId: null,
                ManagerId: employee.ManagerId
            );



            var integrationEvent = new EmployeeCreatedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: "EmployeeCreated",
                MessageType: MessageType.Event,
                MessageVersion: "1.0",
                PublishedBy: "EmployeeService",
                ConsumedBy: Array.Empty<string>(),
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: payload
            );

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "Published EmployeeCreatedEvent for employee {EmployeeId} ({EmployeeNumber})",
                employee.Id,
                employee.EmployeeNumber);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request - event publishing is not critical
            _logger.LogError(
                ex,
                "Failed to publish EmployeeCreatedEvent for employee {EmployeeId}",
                employee.Id);
        }

        return new CreateEmployeeCommandResult(true, employee.Id, null);
    }
}

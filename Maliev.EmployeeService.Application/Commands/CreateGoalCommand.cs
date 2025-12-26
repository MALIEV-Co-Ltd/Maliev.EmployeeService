using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to create a new goal for an employee
/// </summary>
public record CreateGoalCommand(
    Guid EmployeeId,
    string Description,
    string? SuccessCriteria,
    DateTime TargetDate,
    Guid? PerformanceReviewId = null);

/// <summary>
/// Response containing the newly created goal ID
/// </summary>
public record CreateGoalCommandResult(
    bool Success,
    Guid? GoalId,
    string? ErrorMessage);

/// <summary>
/// Handler for CreateGoalCommand - enforces business rules and validations
/// </summary>
public class CreateGoalCommandHandler
{
    private readonly IGoalRepository _goalRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPerformanceReviewRepository _performanceReviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public CreateGoalCommandHandler(
        IGoalRepository goalRepository,
        IEmployeeRepository employeeRepository,
        IPerformanceReviewRepository performanceReviewRepository,
        IUnitOfWork unitOfWork,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _goalRepository = goalRepository;
        _employeeRepository = employeeRepository;
        _performanceReviewRepository = performanceReviewRepository;
        _unitOfWork = unitOfWork;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<CreateGoalCommandResult> HandleAsync(
        CreateGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have PerformanceUpdate permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.PerformanceUpdate, $"employee/{command.EmployeeId}/performance", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to create goals for this employee");
        }
        // Validate employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return new CreateGoalCommandResult(
                false,
                null,
                $"Employee with ID '{command.EmployeeId}' not found");
        }

        if (!employee.IsActive)
        {
            return new CreateGoalCommandResult(
                false,
                null,
                "Cannot create goal for inactive employee");
        }

        // Validate target date is in the future
        if (command.TargetDate <= DateTime.UtcNow.Date)
        {
            return new CreateGoalCommandResult(
                false,
                null,
                "Target date must be in the future");
        }

        // Validate target date is not too far in the future (e.g., max 2 years)
        if (command.TargetDate > DateTime.UtcNow.AddYears(2))
        {
            return new CreateGoalCommandResult(
                false,
                null,
                "Target date cannot be more than 2 years in the future");
        }

        // Validate description is not empty
        if (string.IsNullOrWhiteSpace(command.Description))
        {
            return new CreateGoalCommandResult(
                false,
                null,
                "Goal description is required");
        }

        // Validate performance review exists if provided
        if (command.PerformanceReviewId.HasValue)
        {
            var performanceReview = await _performanceReviewRepository.GetByIdAsync(
                command.PerformanceReviewId.Value,
                cancellationToken);

            if (performanceReview == null)
            {
                return new CreateGoalCommandResult(
                    false,
                    null,
                    $"Performance review with ID '{command.PerformanceReviewId.Value}' not found");
            }

            // Validate performance review belongs to the same employee
            if (performanceReview.EmployeeId != command.EmployeeId)
            {
                return new CreateGoalCommandResult(
                    false,
                    null,
                    "Performance review does not belong to the specified employee");
            }
        }

        // Create the goal entity
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            PerformanceReviewId = command.PerformanceReviewId,
            Description = command.Description,
            SuccessCriteria = command.SuccessCriteria,
            TargetDate = command.TargetDate,
            CompletionStatus = GoalStatus.NotStarted,
            CreatedBy = _currentUserService.PrincipalId,
            CreatedDate = DateTime.UtcNow
        };

        await _goalRepository.AddAsync(goal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateGoalCommandResult(true, goal.Id, null);
    }
}

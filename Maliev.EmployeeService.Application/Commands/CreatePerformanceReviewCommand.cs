using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to create a new performance review (Manager only)
/// </summary>
public record CreatePerformanceReviewCommand(
    Guid EmployeeId,
    Guid ReviewerId,
    ReviewCycle ReviewCycle,
    DateTime ReviewPeriodStart,
    DateTime ReviewPeriodEnd,
    string? SelfAssessment = null);

/// <summary>
/// Response containing the newly created performance review ID
/// </summary>
public record CreatePerformanceReviewCommandResult(
    bool Success,
    Guid? PerformanceReviewId,
    string? ErrorMessage);

/// <summary>
/// Handler for CreatePerformanceReviewCommand - enforces business rules and validations
/// </summary>
public class CreatePerformanceReviewCommandHandler
{
    private readonly IPerformanceReviewRepository _performanceReviewRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;

    public CreatePerformanceReviewCommandHandler(
        IPerformanceReviewRepository performanceReviewRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _performanceReviewRepository = performanceReviewRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<CreatePerformanceReviewCommandResult> HandleAsync(
        CreatePerformanceReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have PerformanceCreate permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.PerformanceCreate, $"employee/{command.EmployeeId}", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to create a performance review for this employee");
        }
        // Validate employee exists
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return new CreatePerformanceReviewCommandResult(
                false,
                null,
                $"Employee with ID '{command.EmployeeId}' not found");
        }

        if (!employee.IsActive)
        {
            return new CreatePerformanceReviewCommandResult(
                false,
                null,
                "Cannot create performance review for inactive employee");
        }

        // Validate reviewer exists
        var reviewer = await _employeeRepository.GetByIdAsync(command.ReviewerId, cancellationToken);
        if (reviewer == null)
        {
            return new CreatePerformanceReviewCommandResult(
                false,
                null,
                $"Reviewer with ID '{command.ReviewerId}' not found");
        }

        if (!reviewer.IsActive)
        {
            return new CreatePerformanceReviewCommandResult(
                false,
                null,
                "Cannot assign inactive employee as reviewer");
        }

        // Validate reviewer is employee's manager or dotted-line manager
        if (employee.ManagerId != command.ReviewerId &&
            employee.DottedLineManagerId != command.ReviewerId)
        {
            return new CreatePerformanceReviewCommandResult(
                false,
                null,
                "Reviewer must be the employee's direct manager or dotted-line manager");
        }

        // Validate review period
        if (command.ReviewPeriodEnd <= command.ReviewPeriodStart)
        {
            return new CreatePerformanceReviewCommandResult(
                false,
                null,
                "Review period end date must be after start date");
        }

        // Validate review period matches cycle
        var daysDifference = (command.ReviewPeriodEnd - command.ReviewPeriodStart).Days;
        var expectedDays = command.ReviewCycle switch
        {
            ReviewCycle.Quarterly => 90,
            ReviewCycle.SemiAnnual => 180,
            ReviewCycle.Annual => 365,
            _ => 0
        };

        if (Math.Abs(daysDifference - expectedDays) > 30) // Allow 30 days variance
        {
            return new CreatePerformanceReviewCommandResult(
                false,
                null,
                $"Review period length ({daysDifference} days) does not match {command.ReviewCycle} cycle (expected ~{expectedDays} days)");
        }

        // Check for overlapping reviews
        var existingReviews = await _performanceReviewRepository.GetByEmployeeAndPeriodAsync(
            command.EmployeeId,
            command.ReviewPeriodStart,
            command.ReviewPeriodEnd,
            cancellationToken);

        if (existingReviews.Any())
        {
            return new CreatePerformanceReviewCommandResult(
                false,
                null,
                "A performance review already exists for this employee in the specified period");
        }

        // Create the performance review entity
        var performanceReview = new PerformanceReview
        {
            Id = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            ReviewerId = command.ReviewerId,
            ReviewCycle = command.ReviewCycle,
            ReviewPeriodStart = command.ReviewPeriodStart,
            ReviewPeriodEnd = command.ReviewPeriodEnd,
            Status = "Draft",
            SelfAssessment = command.SelfAssessment,
            CreatedBy = _currentUserService.PrincipalId,
            CreatedDate = DateTime.UtcNow
        };

        await _performanceReviewRepository.AddAsync(performanceReview, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatePerformanceReviewCommandResult(true, performanceReview.Id, null);
    }
}

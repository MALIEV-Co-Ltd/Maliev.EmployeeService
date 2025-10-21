using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to acknowledge a performance review (Employee acknowledges they have reviewed it)
/// </summary>
public record AcknowledgePerformanceReviewCommand(
    Guid PerformanceReviewId,
    Guid EmployeeId);

/// <summary>
/// Response for performance review acknowledgment
/// </summary>
public record AcknowledgePerformanceReviewCommandResult(
    bool Success,
    string? ErrorMessage);

/// <summary>
/// Handler for AcknowledgePerformanceReviewCommand
/// </summary>
public class AcknowledgePerformanceReviewCommandHandler
{
    private readonly IPerformanceReviewRepository _performanceReviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AcknowledgePerformanceReviewCommandHandler(
        IPerformanceReviewRepository performanceReviewRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _performanceReviewRepository = performanceReviewRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<AcknowledgePerformanceReviewCommandResult> HandleAsync(
        AcknowledgePerformanceReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate performance review exists
        var performanceReview = await _performanceReviewRepository.GetByIdAsync(
            command.PerformanceReviewId,
            cancellationToken);

        if (performanceReview == null)
        {
            return new AcknowledgePerformanceReviewCommandResult(
                false,
                $"Performance review with ID '{command.PerformanceReviewId}' not found");
        }

        // Validate employee is the subject of the review
        if (performanceReview.EmployeeId != command.EmployeeId)
        {
            return new AcknowledgePerformanceReviewCommandResult(
                false,
                "You can only acknowledge your own performance reviews");
        }

        // Validate review is submitted (not draft)
        if (performanceReview.Status == "Draft")
        {
            return new AcknowledgePerformanceReviewCommandResult(
                false,
                "Cannot acknowledge a draft performance review. It must be submitted first.");
        }

        // Validate not already acknowledged
        if (performanceReview.AcknowledgedDate.HasValue)
        {
            return new AcknowledgePerformanceReviewCommandResult(
                false,
                $"Performance review was already acknowledged on {performanceReview.AcknowledgedDate:yyyy-MM-dd}");
        }

        // Validate review has been completed
        if (!performanceReview.ReviewDate.HasValue)
        {
            return new AcknowledgePerformanceReviewCommandResult(
                false,
                "Cannot acknowledge a performance review that has not been completed");
        }

        // Acknowledge the review
        performanceReview.AcknowledgedDate = DateTime.UtcNow;
        performanceReview.Status = "Acknowledged";
        performanceReview.ModifiedBy = _currentUserService.EmployeeId;
        performanceReview.ModifiedDate = DateTime.UtcNow;

        _performanceReviewRepository.Update(performanceReview);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AcknowledgePerformanceReviewCommandResult(true, null);
    }
}

using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetPerformanceReviewsQuery
/// </summary>
public class GetPerformanceReviewsQueryHandler
{
    private readonly IPerformanceReviewRepository _performanceReviewRepository;

    public GetPerformanceReviewsQueryHandler(IPerformanceReviewRepository performanceReviewRepository)
    {
        _performanceReviewRepository = performanceReviewRepository;
    }

    public async Task<GetPerformanceReviewsQueryResult> HandleAsync(
        GetPerformanceReviewsQuery query,
        CancellationToken cancellationToken = default)
    {
        var performanceReviews = await _performanceReviewRepository.GetByEmployeeIdAsync(
            query.EmployeeId,
            cancellationToken);

        var performanceReviewDtos = performanceReviews.Select(pr => new PerformanceReviewDto
        {
            Id = pr.Id,
            EmployeeId = pr.EmployeeId,
            EmployeeName = pr.Employee?.FullName ?? string.Empty,
            ReviewerId = pr.ReviewerId,
            ReviewerName = pr.Reviewer?.FullName ?? string.Empty,
            ReviewCycle = pr.ReviewCycle.ToString(),
            ReviewPeriodStart = pr.ReviewPeriodStart,
            ReviewPeriodEnd = pr.ReviewPeriodEnd,
            Rating = pr.Rating?.ToString(),
            Feedback = pr.Feedback,
            ReviewDate = pr.ReviewDate,
            AcknowledgedDate = pr.AcknowledgedDate,
            Status = pr.Status,
            SelfAssessment = pr.SelfAssessment,
            CreatedDate = pr.CreatedDate,
            ModifiedDate = pr.ModifiedDate
        }).ToList();

        return new GetPerformanceReviewsQueryResult(performanceReviewDtos);
    }
}

using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetPerformanceReviewsQuery
/// </summary>
public class GetPerformanceReviewsQueryHandler
{
    private readonly IPerformanceReviewRepository _performanceReviewRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetPerformanceReviewsQueryHandler(
        IPerformanceReviewRepository performanceReviewRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _performanceReviewRepository = performanceReviewRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<GetPerformanceReviewsQueryResult> HandleAsync(
        GetPerformanceReviewsQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have PerformanceRead permission for this employee
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.EmployeeId}/performance";
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.PerformanceRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view performance reviews for this employee");
        }

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

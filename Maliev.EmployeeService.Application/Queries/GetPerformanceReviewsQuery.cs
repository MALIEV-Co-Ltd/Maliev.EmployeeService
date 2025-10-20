using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get performance reviews for an employee
/// </summary>
public record GetPerformanceReviewsQuery(Guid EmployeeId);

/// <summary>
/// Response containing performance reviews
/// </summary>
public record GetPerformanceReviewsQueryResult(List<PerformanceReviewDto> PerformanceReviews);

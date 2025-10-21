using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetWorkAuthorizationQuery
/// </summary>
public class GetWorkAuthorizationQueryHandler
{
    private readonly IWorkAuthorizationRepository _workAuthorizationRepository;

    public GetWorkAuthorizationQueryHandler(IWorkAuthorizationRepository workAuthorizationRepository)
    {
        _workAuthorizationRepository = workAuthorizationRepository;
    }

    /// <summary>
    /// Handles the query to get work authorizations
    /// </summary>
    public async Task<IEnumerable<WorkAuthorizationDto>> HandleAsync(
        GetWorkAuthorizationQuery query,
        CancellationToken cancellationToken = default)
    {
        var authorizations = await _workAuthorizationRepository.GetByEmployeeIdAsync(
            query.EmployeeId,
            query.IncludeInactive,
            cancellationToken);

        return authorizations.Select(a =>
        {
            var now = DateTime.UtcNow;
            var isExpired = a.ExpirationDate.HasValue && a.ExpirationDate.Value < now;
            var daysUntilExpiration = a.ExpirationDate.HasValue && a.ExpirationDate.Value >= now
                ? (int)(a.ExpirationDate.Value - now).TotalDays
                : (int?)null;

            return new WorkAuthorizationDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                AuthorizationType = a.AuthorizationType,
                DocumentNumber = a.DocumentNumber,
                IssueDate = a.IssueDate,
                ExpirationDate = a.ExpirationDate,
                IssuingAuthority = a.IssuingAuthority,
                SponsorshipStatus = a.SponsorshipStatus,
                RightToWorkDocumentId = a.RightToWorkDocumentId,
                Notes = a.Notes,
                IsActive = a.IsActive,
                IsExpired = isExpired,
                DaysUntilExpiration = daysUntilExpiration
            };
        }).ToList();
    }
}

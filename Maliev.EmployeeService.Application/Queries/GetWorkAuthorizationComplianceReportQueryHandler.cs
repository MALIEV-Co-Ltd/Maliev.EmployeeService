using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetWorkAuthorizationComplianceReportQuery
/// </summary>
public class GetWorkAuthorizationComplianceReportQueryHandler
{
    private readonly IWorkAuthorizationRepository _workAuthorizationRepository;

    public GetWorkAuthorizationComplianceReportQueryHandler(
        IWorkAuthorizationRepository workAuthorizationRepository)
    {
        _workAuthorizationRepository = workAuthorizationRepository;
    }

    /// <summary>
    /// Handles the query to generate compliance report
    /// </summary>
    public async Task<WorkAuthorizationComplianceReportDto> HandleAsync(
        GetWorkAuthorizationComplianceReportQuery query,
        CancellationToken cancellationToken = default)
    {
        // Get expiring and expired authorizations
        var expiring = await _workAuthorizationRepository.GetExpiringAsync(
            query.DaysUntilExpiration,
            cancellationToken);

        var expired = await _workAuthorizationRepository.GetExpiredAsync(cancellationToken);

        // Get sponsorship status summary
        var sponsorshipSummary = await _workAuthorizationRepository
            .GetSponsorshipStatusSummaryAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var report = new WorkAuthorizationComplianceReportDto
        {
            TotalActive = sponsorshipSummary.Values.Sum(),
            ExpiringSoon = expiring.Count(),
            Expired = expired.Count(),
            SponsorshipStatusSummary = sponsorshipSummary,
            ExpiringAuthorizations = expiring.Select(a => new ExpiringAuthorizationDto
            {
                AuthorizationId = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeNumber = a.Employee?.EmployeeNumber ?? "Unknown",
                EmployeeName = a.Employee?.LegalName?.FullName ?? "Unknown",
                AuthorizationType = a.AuthorizationType.ToString(),
                DocumentNumber = a.DocumentNumber,
                ExpirationDate = a.ExpirationDate,
                DaysUntilExpiration = a.ExpirationDate.HasValue
                    ? (int)(a.ExpirationDate.Value - now).TotalDays
                    : null,
                Department = a.Employee?.Department?.Name ?? "Unknown"
            }).ToList(),
            ExpiredAuthorizations = expired.Select(a => new ExpiringAuthorizationDto
            {
                AuthorizationId = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeNumber = a.Employee?.EmployeeNumber ?? "Unknown",
                EmployeeName = a.Employee?.LegalName?.FullName ?? "Unknown",
                AuthorizationType = a.AuthorizationType.ToString(),
                DocumentNumber = a.DocumentNumber,
                ExpirationDate = a.ExpirationDate,
                DaysUntilExpiration = a.ExpirationDate.HasValue
                    ? (int)(a.ExpirationDate.Value - now).TotalDays
                    : null,
                Department = a.Employee?.Department?.Name ?? "Unknown"
            }).ToList()
        };

        return report;
    }
}

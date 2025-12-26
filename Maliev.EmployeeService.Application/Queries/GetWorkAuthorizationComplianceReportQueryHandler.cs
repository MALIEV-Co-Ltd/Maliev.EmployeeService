using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetWorkAuthorizationComplianceReportQuery
/// </summary>
public class GetWorkAuthorizationComplianceReportQueryHandler
{
    private readonly IWorkAuthorizationRepository _workAuthorizationRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetWorkAuthorizationComplianceReportQueryHandler(
        IWorkAuthorizationRepository workAuthorizationRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _workAuthorizationRepository = workAuthorizationRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Handles the query to generate compliance report
    /// </summary>
    public async Task<WorkAuthorizationComplianceReportDto> HandleAsync(
        GetWorkAuthorizationComplianceReportQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have WorkAuthManage permission
        var principalId = _currentUserService.PrincipalId?.ToString();
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.WorkAuthManage, "employee/work-auth/compliance", cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view work authorization compliance reports");
        }

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

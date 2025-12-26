using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetWorkAuthorizationQuery
/// </summary>
public class GetWorkAuthorizationQueryHandler
{
    private readonly IWorkAuthorizationRepository _workAuthorizationRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetWorkAuthorizationQueryHandler(
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
    /// Handles the query to get work authorizations
    /// </summary>
    public async Task<IEnumerable<WorkAuthorizationDto>> HandleAsync(
        GetWorkAuthorizationQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have WorkAuthManage permission for this employee
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.EmployeeId}/work-auth";
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.WorkAuthManage, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view work authorizations for this employee");
        }

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

using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Handler for GetTeamQuery - returns direct reports with pagination
/// </summary>
public class GetTeamQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public GetTeamQueryHandler(
        IEmployeeRepository employeeRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<GetTeamQueryResult> HandleAsync(
        GetTeamQuery query,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have ProfilesRead permission for this manager's team
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.ManagerId}/team";
        if (string.IsNullOrEmpty(principalId) ||
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.ProfilesRead, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view this manager's team");
        }

        // Get all direct reports
        var allDirectReports = await _employeeRepository.GetDirectReportsAsync(
            query.ManagerId,
            cancellationToken);

        var directReportsList = allDirectReports.ToList();
        var totalCount = directReportsList.Count;

        // Apply pagination
        var paginatedReports = directReportsList
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        // Map to DTOs
        var teamMemberDtos = new List<TeamMemberDto>();
        foreach (var employee in paginatedReports)
        {
            // Count direct reports for this employee
            var subReports = await _employeeRepository.GetDirectReportsAsync(
                employee.Id,
                cancellationToken);

            teamMemberDtos.Add(new TeamMemberDto
            {
                EmployeeId = employee.Id,
                EmployeeNumber = employee.EmployeeNumber,
                FullName = employee.FullName,
                PreferredName = employee.PreferredName ?? string.Empty,
                JobTitle = employee.JobTitle ?? string.Empty,
                DepartmentName = employee.Department?.Name ?? string.Empty,
                EmploymentStatus = employee.EmploymentStatus,
                EmploymentType = employee.EmploymentType,
                WorkLocation = employee.WorkLocation ?? string.Empty,
                WorkEmail = employee.ContactInformation.WorkEmail,
                StartDate = employee.StartDate,
                DirectReportsCount = subReports.Count()
            });
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new GetTeamQueryResult(
            teamMemberDtos,
            totalCount,
            query.PageNumber,
            query.PageSize,
            totalPages);
    }
}

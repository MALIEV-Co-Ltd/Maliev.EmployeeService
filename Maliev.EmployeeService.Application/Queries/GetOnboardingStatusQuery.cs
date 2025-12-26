using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get onboarding status for an employee
/// </summary>
public class GetOnboardingStatusQuery
{
    public Guid EmployeeId { get; set; }
}

/// <summary>
/// DTO for onboarding status response
/// </summary>
public class OnboardingStatusDto
{
    public Guid EmployeeId { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<OnboardingChecklistItemDto> ChecklistItems { get; set; } = new();
}

/// <summary>
/// DTO for individual onboarding checklist item
/// </summary>
public class OnboardingChecklistItemDto
{
    public Guid Id { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string ResponsibleParty { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool CompletionStatus { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? CompletedByName { get; set; }
    public string? Notes { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsOverdue { get; set; }
}

/// <summary>
/// Handler for getting onboarding status with checklist and completion percentage
/// </summary>
public class GetOnboardingStatusQueryHandler
{
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IIamServiceClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetOnboardingStatusQueryHandler> _logger;

    public GetOnboardingStatusQueryHandler(
        IOnboardingRepository onboardingRepository,
        IIamServiceClient iamClient,
        IConfiguration configuration,
        ICurrentUserService currentUserService,
        ILogger<GetOnboardingStatusQueryHandler> logger)
    {
        _onboardingRepository = onboardingRepository;
        _iamClient = iamClient;
        _configuration = configuration;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<OnboardingStatusDto> HandleAsync(GetOnboardingStatusQuery query, CancellationToken cancellationToken = default)
    {
        // Authorization check: User must have OnboardingManage permission for this employee
        var principalId = _currentUserService.PrincipalId?.ToString();
        var resourcePath = $"employee/{query.EmployeeId}/onboarding";
        if (string.IsNullOrEmpty(principalId) || 
            !await _iamClient.CheckPermissionAsync(principalId, EmployeePermissions.OnboardingManage, resourcePath, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to view onboarding status for this employee");
        }

        _logger.LogInformation("Getting onboarding status for employee {EmployeeId}", query.EmployeeId);

        // Get status summary
        var status = await _onboardingRepository.GetStatusAsync(query.EmployeeId, cancellationToken);

        // Get detailed checklist
        var checklist = await _onboardingRepository.GetChecklistAsync(query.EmployeeId, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var checklistItems = checklist.Select(item => new OnboardingChecklistItemDto
        {
            Id = item.Id,
            ItemDescription = item.ItemDescription,
            ResponsibleParty = item.ResponsibleParty.ToString(),
            DueDate = item.DueDate,
            CompletionStatus = item.CompletionStatus,
            CompletedDate = item.CompletedDate,
            CompletedByName = item.CompletedByEmployee != null
                ? $"{item.CompletedByEmployee.LegalName.FirstName} {item.CompletedByEmployee.LegalName.LastName}"
                : null,
            Notes = item.Notes,
            DisplayOrder = item.DisplayOrder,
            IsOverdue = !item.CompletionStatus && item.DueDate.Date < today
        }).ToList();

        return new OnboardingStatusDto
        {
            EmployeeId = query.EmployeeId,
            TotalItems = status.TotalCount,
            CompletedItems = status.CompletedCount,
            CompletionPercentage = status.CompletionPercentage,
            ChecklistItems = checklistItems
        };
    }
}

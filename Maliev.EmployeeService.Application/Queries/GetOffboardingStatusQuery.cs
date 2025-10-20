using Maliev.EmployeeService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get offboarding status for an employee
/// </summary>
public class GetOffboardingStatusQuery
{
    public Guid EmployeeId { get; set; }
}

/// <summary>
/// Handler for GetOffboardingStatusQuery
/// Returns offboarding status with checklist and paycheck release validation
/// </summary>
public class GetOffboardingStatusQueryHandler
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOffboardingRepository _offboardingRepository;
    private readonly ILogger<GetOffboardingStatusQueryHandler> _logger;

    public GetOffboardingStatusQueryHandler(
        IEmployeeRepository employeeRepository,
        IOffboardingRepository offboardingRepository,
        ILogger<GetOffboardingStatusQueryHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _offboardingRepository = offboardingRepository;
        _logger = logger;
    }

    public async Task<OffboardingStatusDto> HandleAsync(
        GetOffboardingStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(query.EmployeeId, cancellationToken);
        if (employee == null)
        {
            throw new InvalidOperationException($"Employee {query.EmployeeId} not found");
        }

        // Get offboarding checklist
        var checklist = await _offboardingRepository.GetChecklistAsync(query.EmployeeId, cancellationToken);
        var checklistList = checklist.ToList();

        // Calculate status
        var totalCount = checklistList.Count;
        var completedCount = checklistList.Count(item => item.CompletionStatus);
        var completionPercentage = totalCount > 0
            ? Math.Round((decimal)completedCount / totalCount * 100, 2)
            : 0m;

        // Check if final paycheck can be released (T196)
        var canReleaseFinalPaycheck = await _offboardingRepository.CanFinalizeAsync(
            query.EmployeeId,
            cancellationToken);

        // Get blocking items
        var blockingItems = await _offboardingRepository.GetPaycheckBlockingItemsAsync(
            query.EmployeeId,
            cancellationToken);

        return new OffboardingStatusDto
        {
            EmployeeId = query.EmployeeId,
            TotalItems = totalCount,
            CompletedItems = completedCount,
            CompletionPercentage = completionPercentage,
            CanReleaseFinalPaycheck = canReleaseFinalPaycheck,
            BlockingItemsCount = blockingItems.Count(item => !item.CompletionStatus),
            ChecklistItems = checklistList.Select(item => new OffboardingChecklistItemDto
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
                BlocksFinalPaycheck = item.BlocksFinalPaycheck,
                IsOverdue = !item.CompletionStatus && item.DueDate < DateTime.UtcNow.Date
            })
            .OrderBy(item => item.DisplayOrder)
            .ToList()
        };
    }
}

/// <summary>
/// DTO for offboarding status response
/// </summary>
public class OffboardingStatusDto
{
    public Guid EmployeeId { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public decimal CompletionPercentage { get; set; }
    public bool CanReleaseFinalPaycheck { get; set; }
    public int BlockingItemsCount { get; set; }
    public List<OffboardingChecklistItemDto> ChecklistItems { get; set; } = new();
}

/// <summary>
/// DTO for offboarding checklist item
/// </summary>
public class OffboardingChecklistItemDto
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
    public bool BlocksFinalPaycheck { get; set; }
    public bool IsOverdue { get; set; }
}

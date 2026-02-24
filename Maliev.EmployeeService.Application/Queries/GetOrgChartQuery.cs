using Maliev.EmployeeService.Application.DTOs;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get organizational chart starting from a manager (User Story 3)
/// Returns hierarchical structure with depth limit
/// </summary>
public record GetOrgChartQuery(
    Guid ManagerId,
    int MaxDepth = 3);

/// <summary>
/// Response containing hierarchical org chart
/// </summary>
public record GetOrgChartQueryResult(OrgChartDto? OrgChart);

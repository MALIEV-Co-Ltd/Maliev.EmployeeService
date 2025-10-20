namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for employee search results with pagination
/// User Story 12 - Reporting & Analytics
/// </summary>
public class EmployeeSearchResultDto
{
    /// <summary>
    /// List of matching employees
    /// </summary>
    public List<EmployeeSearchItemDto> Results { get; set; } = new();

    /// <summary>
    /// Total number of matching records
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Individual employee search result item
/// </summary>
public class EmployeeSearchItemDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? ManagerName { get; set; }
    public Guid? ManagerId { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
}

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to search employees with multi-criteria filtering
/// User Story 12 - Reporting & Analytics
/// </summary>
public class SearchEmployeesQuery
{
    /// <summary>
    /// Search by employee name (first, last, or preferred)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Search by employee number
    /// </summary>
    public string? EmployeeNumber { get; set; }

    /// <summary>
    /// Search by email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Filter by department ID
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Filter by job title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Filter by employment status
    /// </summary>
    public string? EmploymentStatus { get; set; }

    /// <summary>
    /// Filter by manager ID
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (default: 50, max: 200)
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort field (name, employeeNumber, hireDate, department)
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string? SortDirection { get; set; } = "asc";
}

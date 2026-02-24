using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to export employee data to CSV format
/// User Story 12 - Bulk Operations
/// </summary>
public class ExportEmployeesQuery
{
    /// <summary>
    /// Optional department filter
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Optional employment status filter
    /// </summary>
    public EmploymentStatus? EmploymentStatus { get; set; }

    /// <summary>
    /// Include terminated employees (default: false)
    /// </summary>
    public bool IncludeTerminated { get; set; } = false;

    /// <summary>
    /// Include sensitive data like salary (requires admin/HR role)
    /// </summary>
    public bool IncludeSalaryData { get; set; } = false;

    /// <summary>
    /// Include personal contact information
    /// </summary>
    public bool IncludePersonalData { get; set; } = true;
}

namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// Result of employee export operation
/// User Story 12 - Bulk Operations
/// </summary>
public class ExportEmployeesResultDto
{
    /// <summary>
    /// CSV content as string
    /// </summary>
    public required string CsvContent { get; set; }

    /// <summary>
    /// Number of employees exported
    /// </summary>
    public int TotalEmployees { get; set; }

    /// <summary>
    /// Suggested filename for download
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// When the export was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

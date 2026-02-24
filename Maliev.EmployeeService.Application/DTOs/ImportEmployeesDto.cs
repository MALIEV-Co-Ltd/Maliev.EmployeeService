namespace Maliev.EmployeeService.Application.DTOs;

/// <summary>
/// DTO for bulk employee import request
/// User Story 12 - Bulk Operations
/// </summary>
public class ImportEmployeesDto
{
    /// <summary>
    /// CSV content to import
    /// </summary>
    public required string CsvContent { get; set; }

    /// <summary>
    /// Skip validation errors and import valid rows only
    /// </summary>
    public bool SkipInvalidRows { get; set; } = false;

    /// <summary>
    /// Dry run mode - validate only, don't import
    /// </summary>
    public bool DryRun { get; set; } = false;
}

/// <summary>
/// Result of import operation
/// </summary>
public class ImportEmployeesResultDto
{
    /// <summary>
    /// Job ID for tracking async import
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Message describing the operation
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Total rows in CSV (excluding header)
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Status tracking URL
    /// </summary>
    public string? StatusUrl { get; set; }
}

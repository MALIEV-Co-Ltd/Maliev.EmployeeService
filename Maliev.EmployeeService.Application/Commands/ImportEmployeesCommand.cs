namespace Maliev.EmployeeService.Application.Commands;

/// <summary>
/// Command to import employees from CSV
/// User Story 12 - Bulk Operations
/// </summary>
public class ImportEmployeesCommand
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

    /// <summary>
    /// User initiating the import
    /// </summary>
    public required Guid InitiatedByPrincipalId { get; set; }
}

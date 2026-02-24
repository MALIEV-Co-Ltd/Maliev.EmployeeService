namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get the status of a bulk job
/// User Story 12 - Bulk Operations
/// </summary>
public class GetBulkJobStatusQuery
{
    /// <summary>
    /// Job ID to retrieve status for
    /// </summary>
    public required Guid JobId { get; set; }
}

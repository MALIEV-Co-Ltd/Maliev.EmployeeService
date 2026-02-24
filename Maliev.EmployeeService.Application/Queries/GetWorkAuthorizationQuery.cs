namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to get work authorization details for an employee
/// </summary>
public class GetWorkAuthorizationQuery
{
    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Include inactive authorizations
    /// </summary>
    public bool IncludeInactive { get; set; } = false;
}

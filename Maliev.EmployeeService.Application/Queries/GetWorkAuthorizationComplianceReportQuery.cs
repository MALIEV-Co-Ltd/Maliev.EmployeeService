namespace Maliev.EmployeeService.Application.Queries;

/// <summary>
/// Query to generate work authorization compliance report
/// </summary>
public class GetWorkAuthorizationComplianceReportQuery
{
    /// <summary>
    /// Days until expiration threshold for "expiring soon" category
    /// </summary>
    public int DaysUntilExpiration { get; set; } = 90;
}

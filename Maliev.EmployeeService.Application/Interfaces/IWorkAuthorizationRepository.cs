using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository for WorkAuthorization entity operations
/// </summary>
public interface IWorkAuthorizationRepository : IRepository<WorkAuthorization>
{
    /// <summary>
    /// Gets all work authorizations for a specific employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="includeInactive">Include inactive authorizations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of work authorizations</returns>
    Task<IEnumerable<WorkAuthorization>> GetByEmployeeIdAsync(
        Guid employeeId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all work authorizations expiring within the specified number of days
    /// </summary>
    /// <param name="daysUntilExpiration">Number of days until expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expiring work authorizations</returns>
    Task<IEnumerable<WorkAuthorization>> GetExpiringAsync(
        int daysUntilExpiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all expired work authorizations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired work authorizations</returns>
    Task<IEnumerable<WorkAuthorization>> GetExpiredAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active work authorization for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active work authorization or null</returns>
    Task<WorkAuthorization?> GetActiveByEmployeeIdAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work authorizations grouped by sponsorship status for compliance reporting
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of sponsorship status to authorization count</returns>
    Task<Dictionary<string, int>> GetSponsorshipStatusSummaryAsync(
        CancellationToken cancellationToken = default);
}

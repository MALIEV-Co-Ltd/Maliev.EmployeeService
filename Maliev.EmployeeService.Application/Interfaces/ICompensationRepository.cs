using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for CompensationRecord entity with specialized query methods
/// </summary>
public interface ICompensationRepository : IRepository<CompensationRecord>
{
    /// <summary>
    /// Gets the current (most recent) compensation record for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current compensation record or null if none exists</returns>
    Task<CompensationRecord?> GetCurrentAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete compensation history for an employee ordered by effective date descending
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of compensation records</returns>
    Task<IEnumerable<CompensationRecord>> GetHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new compensation record (async wrapper for AddAsync)
    /// </summary>
    /// <param name="compensationRecord">Compensation record to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CreateAsync(CompensationRecord compensationRecord, CancellationToken cancellationToken = default);
}

using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for BenefitsEnrollment entity with specialized query methods
/// </summary>
public interface IBenefitsRepository : IRepository<BenefitsEnrollment>
{
    /// <summary>
    /// Gets the current (most recent) benefits enrollment for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current benefits enrollment or null if none exists</returns>
    Task<BenefitsEnrollment?> GetEnrollmentAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing benefits enrollment
    /// </summary>
    /// <param name="enrollment">Benefits enrollment to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateEnrollmentAsync(BenefitsEnrollment enrollment, CancellationToken cancellationToken = default);
}

using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for training record management
/// </summary>
public interface ITrainingRepository
{
    /// <summary>
    /// Creates a new training record
    /// </summary>
    Task<TrainingRecord> CreateAsync(TrainingRecord trainingRecord, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all training records for an employee
    /// </summary>
    Task<IEnumerable<TrainingRecord>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets training records by type (Mandatory or Voluntary)
    /// </summary>
    Task<IEnumerable<TrainingRecord>> GetByTypeAsync(Guid employeeId, TrainingType trainingType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets certifications expiring within specified days
    /// </summary>
    Task<IEnumerable<TrainingRecord>> GetExpiringCertificationsAsync(int daysFromNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all expired certifications
    /// </summary>
    Task<IEnumerable<TrainingRecord>> GetExpiredCertificationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a training record by ID
    /// </summary>
    Task<TrainingRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a training record
    /// </summary>
    Task UpdateAsync(TrainingRecord trainingRecord, CancellationToken cancellationToken = default);
}

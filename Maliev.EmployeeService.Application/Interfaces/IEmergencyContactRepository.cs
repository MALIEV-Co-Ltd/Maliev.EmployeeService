using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for EmergencyContact entity
/// </summary>
public interface IEmergencyContactRepository : IRepository<EmergencyContact>
{
    /// <summary>
    /// Get all emergency contacts for an employee, ordered by priority
    /// </summary>
    Task<IEnumerable<EmergencyContact>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get primary emergency contact for an employee (priority order = 1)
    /// </summary>
    Task<EmergencyContact?> GetPrimaryContactAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update priority order for multiple contacts (used when reordering)
    /// </summary>
    Task UpdatePriorityOrderAsync(IEnumerable<(Guid ContactId, int NewPriority)> updates, CancellationToken cancellationToken = default);
}

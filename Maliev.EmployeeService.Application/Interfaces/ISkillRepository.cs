using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for employee skill management
/// </summary>
public interface ISkillRepository
{
    /// <summary>
    /// Creates a new skill record
    /// </summary>
    Task<Skill> CreateAsync(Skill skill, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all skills for an employee
    /// </summary>
    Task<IEnumerable<Skill>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets skills marked as development areas for an employee
    /// </summary>
    Task<IEnumerable<Skill>> GetDevelopmentAreasAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a skill by ID
    /// </summary>
    Task<Skill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a skill record
    /// </summary>
    Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a skill record
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets skills by name across all employees (for skills matrix reporting)
    /// </summary>
    Task<IEnumerable<Skill>> GetBySkillNameAsync(string skillName, CancellationToken cancellationToken = default);
}

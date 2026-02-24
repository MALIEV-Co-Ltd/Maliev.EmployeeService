using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for Department entity with hierarchical query support
/// </summary>
public interface IDepartmentRepository : IRepository<Department>
{
    /// <summary>
    /// Gets all departments with their immediate subdepartments
    /// </summary>
    Task<IEnumerable<Department>> GetAllWithSubDepartmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a department by ID with all its subdepartments loaded
    /// </summary>
    Task<Department?> GetByIdWithSubDepartmentsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete hierarchical tree of departments starting from root departments
    /// Returns a flat list with navigation properties populated
    /// </summary>
    Task<IEnumerable<Department>> GetHierarchyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subdepartments (at any level) under a given department
    /// </summary>
    Task<IEnumerable<Department>> GetAllSubDepartmentsRecursiveAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active employees in a department
    /// </summary>
    Task<int> GetEmployeeCountAsync(Guid departmentId, bool includeSubDepartments = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a department has any subdepartments
    /// </summary>
    Task<bool> HasSubDepartmentsAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a department has any active employees assigned
    /// </summary>
    Task<bool> HasEmployeesAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets departments that are approaching or at their headcount limit
    /// </summary>
    Task<IEnumerable<Department>> GetDepartmentsNearHeadcountLimitAsync(CancellationToken cancellationToken = default);
}

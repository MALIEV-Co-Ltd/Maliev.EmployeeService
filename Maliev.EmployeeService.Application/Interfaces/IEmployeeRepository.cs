using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for Employee entity with specialized query methods
/// </summary>
public interface IEmployeeRepository : IRepository<Employee>
{
    /// <summary>
    /// Get employee by employee number
    /// </summary>
    Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employee by IAM Principal ID
    /// </summary>
    Task<Employee?> GetByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employee by work email address (case-insensitive).
    /// Used by AuthService for Google SSO identity resolution.
    /// </summary>
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employee with emergency contacts included
    /// </summary>
    Task<Employee?> GetWithEmergencyContactsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employee with manager and department included
    /// </summary>
    Task<Employee?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all direct reports for a manager
    /// </summary>
    Task<IEnumerable<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employees by department
    /// </summary>
    Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employees by employment status
    /// </summary>
    Task<IEnumerable<Employee>> GetByStatusAsync(EmploymentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employees by employment type
    /// </summary>
    Task<IEnumerable<Employee>> GetByTypeAsync(EmploymentType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search employees by name (first, last, or preferred)
    /// </summary>
    Task<IEnumerable<Employee>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of active employees in a department
    /// </summary>
    Task<int> GetCountByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employees with start date in specified date range (inclusive)
    /// </summary>
    Task<IEnumerable<Employee>> GetEmployeesByStartDateAsync(DateTime startDateFrom, DateTime startDateTo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employees with termination date on or before specified date
    /// </summary>
    Task<IEnumerable<Employee>> GetEmployeesByTerminationDateAsync(DateTime terminationDate, CancellationToken cancellationToken = default);
}

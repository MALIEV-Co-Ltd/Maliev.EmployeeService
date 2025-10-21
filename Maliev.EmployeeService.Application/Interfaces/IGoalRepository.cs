using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for Goal entity
/// Supports employee goal and objective management
/// </summary>
public interface IGoalRepository : IRepository<Goal>
{
    /// <summary>
    /// Get all goals for an employee
    /// </summary>
    Task<IEnumerable<Goal>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get goals associated with a specific performance review
    /// </summary>
    Task<IEnumerable<Goal>> GetByPerformanceReviewIdAsync(Guid performanceReviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get goals by completion status
    /// </summary>
    Task<IEnumerable<Goal>> GetByStatusAsync(GoalStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get goals within a target date range
    /// </summary>
    Task<IEnumerable<Goal>> GetByTargetDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get overdue goals (target date passed but status not Completed)
    /// </summary>
    Task<IEnumerable<Goal>> GetOverdueGoalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get overdue goals for a specific employee
    /// </summary>
    Task<IEnumerable<Goal>> GetOverdueGoalsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get goals for an employee by status
    /// </summary>
    Task<IEnumerable<Goal>> GetByEmployeeAndStatusAsync(Guid employeeId, GoalStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active goals for an employee (InProgress status)
    /// </summary>
    Task<IEnumerable<Goal>> GetActiveGoalsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get goals for a team (all goals for employees under a manager)
    /// </summary>
    Task<IEnumerable<Goal>> GetTeamGoalsAsync(Guid managerId, CancellationToken cancellationToken = default);
}

using Maliev.EmployeeService.Domain.Entities;

namespace Maliev.EmployeeService.Application.Interfaces;

/// <summary>
/// Repository interface for Team entity (User Story 5 - Matrix Organizations)
/// </summary>
public interface ITeamRepository : IRepository<Team>
{
    /// <summary>
    /// Gets a team by ID with all team members loaded
    /// </summary>
    Task<Team?> GetWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a team by ID with team lead loaded
    /// </summary>
    Task<Team?> GetWithTeamLeadAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active teams
    /// </summary>
    Task<IEnumerable<Team>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets teams by team type
    /// </summary>
    Task<IEnumerable<Team>> GetByTeamTypeAsync(string teamType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets teams where the specified employee is the team lead
    /// </summary>
    Task<IEnumerable<Team>> GetByTeamLeadAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets teams where the specified employee is a member
    /// </summary>
    Task<IEnumerable<Team>> GetTeamsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an employee is a member of a specific team
    /// </summary>
    Task<bool> IsEmployeeMemberAsync(Guid employeeId, Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary team for an employee
    /// </summary>
    Task<Team?> GetPrimaryTeamAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

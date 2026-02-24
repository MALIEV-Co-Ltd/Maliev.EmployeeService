using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Team entity (User Story 5 - Matrix Organizations)
/// </summary>
public class TeamRepository : Repository<Team>, ITeamRepository
{
    public TeamRepository(EmployeeDbContext context) : base(context)
    {
    }

    public async Task<Team?> GetWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.TeamMembers)
                .ThenInclude(tm => tm.Employee)
            .Include(t => t.TeamLead)
            .AsSplitQuery() // Phase 16 - T388: Prevent cartesian explosion with TeamMembers collection
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
    }

    public async Task<Team?> GetWithTeamLeadAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.TeamLead)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
    }

    public async Task<IEnumerable<Team>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsActive)
            .Include(t => t.TeamLead)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Team>> GetByTeamTypeAsync(string teamType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TeamType == teamType && t.IsActive)
            .Include(t => t.TeamLead)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Team>> GetByTeamLeadAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TeamLeadId == employeeId && t.IsActive)
            .Include(t => t.TeamLead)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Team>> GetTeamsByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.TeamMembers.Any(tm => tm.EmployeeId == employeeId) && t.IsActive)
            .Include(t => t.TeamLead)
            .Include(t => t.TeamMembers.Where(tm => tm.EmployeeId == employeeId))
            .AsSplitQuery() // Phase 16 - T388: Prevent cartesian explosion with filtered TeamMembers collection
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsEmployeeMemberAsync(Guid employeeId, Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeTeamAssignments
            .AnyAsync(eta => eta.EmployeeId == employeeId && eta.TeamId == teamId, cancellationToken);
    }

    public async Task<Team?> GetPrimaryTeamAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var primaryAssignment = await _context.EmployeeTeamAssignments
            .Include(eta => eta.Team)
                .ThenInclude(t => t.TeamLead)
            .Where(eta => eta.EmployeeId == employeeId && eta.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        return primaryAssignment?.Team;
    }
}

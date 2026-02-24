using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Employee entity
/// </summary>
public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(EmployeeDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<Employee?> GetByEmployeeNumberAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Employee?> GetByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.PrincipalId == principalId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => EF.Functions.ILike(e.ContactInformation.WorkEmail, email), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Employee?> GetWithEmergencyContactsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.EmergencyContacts.OrderBy(ec => ec.PriorityOrder))
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Employee?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Manager)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.ManagerId == managerId)
            .OrderBy(e => e.LegalName.LastName)
            .ThenBy(e => e.LegalName.FirstName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.DepartmentId == departmentId)
            .OrderBy(e => e.LegalName.LastName)
            .ThenBy(e => e.LegalName.FirstName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Employee>> GetByStatusAsync(EmploymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.EmploymentStatus == status)
            .OrderBy(e => e.LegalName.LastName)
            .ThenBy(e => e.LegalName.FirstName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Employee>> GetByTypeAsync(EmploymentType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.EmploymentType == type)
            .OrderBy(e => e.LegalName.LastName)
            .ThenBy(e => e.LegalName.FirstName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Employee>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .Where(e =>
                e.LegalName.FirstName.ToLower().Contains(lowerSearchTerm) ||
                e.LegalName.LastName.ToLower().Contains(lowerSearchTerm) ||
                (e.PreferredName != null && e.PreferredName.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(e => e.LegalName.LastName)
            .ThenBy(e => e.LegalName.FirstName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetCountByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(e => e.DepartmentId == departmentId && e.EmploymentStatus == EmploymentStatus.Active, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Employee>> GetEmployeesByStartDateAsync(DateTime startDateFrom, DateTime startDateTo, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Manager)
            .Include(e => e.Department)
            .Where(e => e.StartDate >= startDateFrom && e.StartDate < startDateTo)
            .OrderBy(e => e.StartDate)
            .ThenBy(e => e.LegalName.LastName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Employee>> GetEmployeesByTerminationDateAsync(DateTime terminationDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Manager)
            .Include(e => e.Department)
            .Where(e => e.TerminationDate.HasValue && e.TerminationDate.Value.Date <= terminationDate.Date)
            .OrderBy(e => e.TerminationDate)
            .ThenBy(e => e.LegalName.LastName)
            .ToListAsync(cancellationToken);
    }
}

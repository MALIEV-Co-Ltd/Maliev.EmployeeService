using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for EmergencyContact entity
/// </summary>
public class EmergencyContactRepository : Repository<EmergencyContact>, IEmergencyContactRepository
{
    public EmergencyContactRepository(EmployeeServiceDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EmergencyContact>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ec => ec.EmployeeId == employeeId)
            .OrderBy(ec => ec.PriorityOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<EmergencyContact?> GetPrimaryContactAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ec => ec.EmployeeId == employeeId && ec.PriorityOrder == 1)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdatePriorityOrderAsync(IEnumerable<(Guid ContactId, int NewPriority)> updates, CancellationToken cancellationToken = default)
    {
        foreach (var (contactId, newPriority) in updates)
        {
            var contact = await _dbSet.FindAsync(new object[] { contactId }, cancellationToken);
            if (contact != null)
            {
                contact.PriorityOrder = newPriority;
            }
        }
    }
}

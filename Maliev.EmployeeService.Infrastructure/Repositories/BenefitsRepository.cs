using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for BenefitsEnrollment entity
/// </summary>
public class BenefitsRepository : Repository<BenefitsEnrollment>, IBenefitsRepository
{
    public BenefitsRepository(EmployeeDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<BenefitsEnrollment?> GetEnrollmentAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(be => be.EmployeeId == employeeId)
            .OrderByDescending(be => be.EnrollmentDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateEnrollmentAsync(BenefitsEnrollment enrollment, CancellationToken cancellationToken = default)
    {
        Update(enrollment);
        await Task.CompletedTask;
    }
}

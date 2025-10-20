using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for mandatory training requirements
/// </summary>
public class MandatoryTrainingRepository : IMandatoryTrainingRepository
{
    private readonly EmployeeServiceDbContext _context;

    public MandatoryTrainingRepository(EmployeeServiceDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MandatoryTrainingRequirement>> GetActiveRequirementsForEmployeeAsync(
        EmploymentType employmentType,
        string? jobTitle,
        CancellationToken cancellationToken = default)
    {
        return await _context.MandatoryTrainingRequirements
            .Where(r => r.IsActive
                     && (r.EmploymentType == null || r.EmploymentType == employmentType)
                     && (r.JobRole == null || r.JobRole == jobTitle))
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);
    }
}

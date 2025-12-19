using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for employee skill management
/// </summary>
public class SkillRepository : ISkillRepository
{
    private readonly EmployeeDbContext _context;

    public SkillRepository(EmployeeDbContext context)
    {
        _context = context;
    }

    public async Task<Skill> CreateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync(cancellationToken);
        return skill;
    }

    public async Task<IEnumerable<Skill>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Skills
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.ProficiencyLevel)
            .ThenBy(s => s.SkillName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Skill>> GetDevelopmentAreasAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Skills
            .Where(s => s.EmployeeId == employeeId && s.IsDevelopmentArea)
            .OrderBy(s => s.SkillName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Skill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Skills
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        skill.ModifiedDate = DateTime.UtcNow;
        _context.Skills.Update(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var skill = await GetByIdAsync(id, cancellationToken);
        if (skill != null)
        {
            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<Skill>> GetBySkillNameAsync(string skillName, CancellationToken cancellationToken = default)
    {
        return await _context.Skills
            .Include(s => s.Employee)
            .Where(s => s.SkillName.ToLower() == skillName.ToLower())
            .OrderByDescending(s => s.ProficiencyLevel)
            .ThenBy(s => s.Employee.LegalName.LastName)
            .ToListAsync(cancellationToken);
    }
}

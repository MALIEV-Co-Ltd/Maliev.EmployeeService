using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Department entity with hierarchical query support
/// </summary>
public class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(EmployeeDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Department>> GetAllWithSubDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .Include(d => d.SubDepartments)
            .Include(d => d.DepartmentHead)
            .Include(d => d.ParentDepartment)
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .AsSplitQuery() // Phase 16 - T388: Prevent cartesian explosion with collection Include
            .ToListAsync(cancellationToken);
    }

    public async Task<Department?> GetByIdWithSubDepartmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .Include(d => d.SubDepartments)
            .Include(d => d.DepartmentHead)
            .Include(d => d.ParentDepartment)
            .Include(d => d.Employees)
            .AsSplitQuery() // Phase 16 - T388: CRITICAL - Two collections (SubDepartments + Employees) would cause cartesian explosion
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Department>> GetHierarchyAsync(CancellationToken cancellationToken = default)
    {
        // Get all departments with their navigation properties
        var allDepartments = await _context.Departments
            .Include(d => d.DepartmentHead)
            .Include(d => d.ParentDepartment)
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        // Manually build the hierarchy by populating SubDepartments collections
        var departmentDict = allDepartments.ToDictionary(d => d.Id);

        foreach (var dept in allDepartments)
        {
            if (dept.ParentDepartmentId.HasValue && departmentDict.ContainsKey(dept.ParentDepartmentId.Value))
            {
                var parent = departmentDict[dept.ParentDepartmentId.Value];
                if (!parent.SubDepartments.Contains(dept))
                {
                    parent.SubDepartments.Add(dept);
                }
            }
        }

        return allDepartments;
    }

    public async Task<IEnumerable<Department>> GetAllSubDepartmentsRecursiveAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        // LINQ-based recursive approach that works with both InMemory and real databases
        var result = new List<Department>();
        var visited = new HashSet<Guid>();

        // Load all active departments into memory to avoid multiple queries
        var allDepartments = await _context.Departments
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        var departmentDict = allDepartments.ToDictionary(d => d.Id);

        // Recursive helper function
        void CollectSubDepartments(Guid parentId)
        {
            var directChildren = allDepartments
                .Where(d => d.ParentDepartmentId == parentId)
                .ToList();

            foreach (var child in directChildren)
            {
                if (!visited.Contains(child.Id))
                {
                    visited.Add(child.Id);
                    result.Add(child);
                    CollectSubDepartments(child.Id);
                }
            }
        }

        CollectSubDepartments(departmentId);

        return result.OrderBy(d => d.Name).ToList();
    }

    public async Task<int> GetEmployeeCountAsync(Guid departmentId, bool includeSubDepartments = false, CancellationToken cancellationToken = default)
    {
        if (!includeSubDepartments)
        {
            return await _context.Employees
                .Where(e => e.DepartmentId == departmentId && e.EmploymentStatus == Domain.Enums.EmploymentStatus.Active)
                .CountAsync(cancellationToken);
        }

        // Get all subdepartment IDs recursively
        var subDepartments = await GetAllSubDepartmentsRecursiveAsync(departmentId, cancellationToken);
        var departmentIds = subDepartments.Select(d => d.Id).Append(departmentId).ToList();

        return await _context.Employees
            .Where(e => e.DepartmentId.HasValue &&
                        departmentIds.Contains(e.DepartmentId.Value) &&
                        e.EmploymentStatus == Domain.Enums.EmploymentStatus.Active)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> HasSubDepartmentsAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AnyAsync(d => d.ParentDepartmentId == departmentId && d.IsActive, cancellationToken);
    }

    public async Task<bool> HasEmployeesAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AnyAsync(e => e.DepartmentId == departmentId && e.EmploymentStatus == Domain.Enums.EmploymentStatus.Active, cancellationToken);
    }

    public async Task<IEnumerable<Department>> GetDepartmentsNearHeadcountLimitAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments
            .Include(d => d.Employees)
            .Include(d => d.DepartmentHead)
            .Where(d => d.IsActive && d.HeadcountLimit.HasValue)
            .AsSplitQuery() // Phase 16 - T388: Prevent cartesian explosion with collection Include
            .ToListAsync(cancellationToken);

        // Filter in memory to use computed properties
        return departments
            .Where(d => d.IsApproachingHeadcountLimit || d.IsAtHeadcountLimit)
            .OrderByDescending(d => d.CurrentHeadcount)
            .ToList();
    }
}

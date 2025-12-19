using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for onboarding checklist management
/// </summary>
public class OnboardingRepository : IOnboardingRepository
{
    private readonly EmployeeDbContext _context;

    public OnboardingRepository(EmployeeDbContext context)
    {
        _context = context;
    }

    public async Task CreateChecklistAsync(IEnumerable<OnboardingChecklist> checklistItems, CancellationToken cancellationToken = default)
    {
        await _context.OnboardingChecklists.AddRangeAsync(checklistItems, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<OnboardingChecklist>> GetChecklistAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.OnboardingChecklists
            .Where(c => c.EmployeeId == employeeId)
            .OrderBy(c => c.DisplayOrder)
            .Include(c => c.CompletedByEmployee)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int CompletedCount, int TotalCount, decimal CompletionPercentage)> GetStatusAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var checklist = await _context.OnboardingChecklists
            .Where(c => c.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var totalCount = checklist.Count;
        var completedCount = checklist.Count(c => c.CompletionStatus);

        var completionPercentage = totalCount > 0
            ? Math.Round((decimal)completedCount / totalCount * 100, 2)
            : 0m;

        return (completedCount, totalCount, completionPercentage);
    }

    public async Task CompleteItemAsync(Guid itemId, Guid completedByEmployeeId, string? notes, CancellationToken cancellationToken = default)
    {
        var item = await _context.OnboardingChecklists
            .FirstOrDefaultAsync(c => c.Id == itemId, cancellationToken);

        if (item == null)
        {
            throw new InvalidOperationException($"Onboarding checklist item {itemId} not found");
        }

        item.CompletionStatus = true;
        item.CompletedDate = DateTime.UtcNow;
        item.CompletedBy = completedByEmployeeId;
        item.Notes = notes;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OnboardingChecklist?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return await _context.OnboardingChecklists
            .Include(c => c.Employee)
            .Include(c => c.CompletedByEmployee)
            .FirstOrDefaultAsync(c => c.Id == itemId, cancellationToken);
    }

    public async Task<IEnumerable<OnboardingChecklist>> GetOverdueItemsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await _context.OnboardingChecklists
            .Where(c => !c.CompletionStatus && c.DueDate < today)
            .Include(c => c.Employee)
            .OrderBy(c => c.DueDate)
            .ToListAsync(cancellationToken);
    }
}

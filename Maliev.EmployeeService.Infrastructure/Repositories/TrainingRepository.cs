using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for training record management
/// </summary>
public class TrainingRepository : ITrainingRepository
{
    private readonly EmployeeDbContext _context;

    public TrainingRepository(EmployeeDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingRecord> CreateAsync(TrainingRecord trainingRecord, CancellationToken cancellationToken = default)
    {
        // Update status based on expiration date
        trainingRecord.UpdateStatus();

        _context.TrainingRecords.Add(trainingRecord);
        await _context.SaveChangesAsync(cancellationToken);
        return trainingRecord;
    }

    public async Task<IEnumerable<TrainingRecord>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingRecords
            .Where(tr => tr.EmployeeId == employeeId)
            .OrderByDescending(tr => tr.CompletionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TrainingRecord>> GetByTypeAsync(Guid employeeId, TrainingType trainingType, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingRecords
            .Where(tr => tr.EmployeeId == employeeId && tr.TrainingType == trainingType)
            .OrderByDescending(tr => tr.CompletionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TrainingRecord>> GetExpiringCertificationsAsync(int daysFromNow, CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.UtcNow.AddDays(daysFromNow);

        return await _context.TrainingRecords
            .Include(tr => tr.Employee)
            .Where(tr => tr.ExpirationDate != null
                      && tr.ExpirationDate <= targetDate
                      && tr.ExpirationDate >= DateTime.UtcNow
                      && tr.Status == CertificationStatus.Expiring)
            .OrderBy(tr => tr.ExpirationDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TrainingRecord>> GetExpiredCertificationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TrainingRecords
            .Include(tr => tr.Employee)
            .Where(tr => tr.Status == CertificationStatus.Expired)
            .OrderBy(tr => tr.ExpirationDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<TrainingRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingRecords
            .Include(tr => tr.Employee)
            .FirstOrDefaultAsync(tr => tr.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(TrainingRecord trainingRecord, CancellationToken cancellationToken = default)
    {
        // Update status based on expiration date
        trainingRecord.UpdateStatus();
        trainingRecord.ModifiedDate = DateTime.UtcNow;

        _context.TrainingRecords.Update(trainingRecord);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

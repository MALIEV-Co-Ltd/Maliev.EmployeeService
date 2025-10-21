using Microsoft.EntityFrameworkCore;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Infrastructure.Data;

namespace Maliev.EmployeeService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for CompensationRecord entity
/// Encryption handled by EncryptionInterceptor on save, decryption handled manually on retrieval
/// </summary>
public class CompensationRepository : Repository<CompensationRecord>, ICompensationRepository
{
    private readonly IEncryptionService _encryptionService;

    public CompensationRepository(EmployeeServiceDbContext context, IEncryptionService encryptionService) : base(context)
    {
        _encryptionService = encryptionService;
    }

    /// <inheritdoc/>
    public async Task<CompensationRecord?> GetCurrentAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var record = await _dbSet
            .Where(cr => cr.EmployeeId == employeeId)
            .OrderByDescending(cr => cr.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (record != null)
        {
            DecryptRecord(record);
        }

        return record;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CompensationRecord>> GetHistoryAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var records = await _dbSet
            .Where(cr => cr.EmployeeId == employeeId)
            .OrderByDescending(cr => cr.EffectiveDate)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            DecryptRecord(record);
        }

        return records;
    }

    /// <inheritdoc/>
    public async Task CreateAsync(CompensationRecord compensationRecord, CancellationToken cancellationToken = default)
    {
        await AddAsync(compensationRecord, cancellationToken);
    }

    /// <summary>
    /// Decrypts the SalaryAmount field if it's encrypted
    /// </summary>
    private void DecryptRecord(CompensationRecord record)
    {
        if (!string.IsNullOrEmpty(record.SalaryAmount) && _encryptionService.IsEncrypted(record.SalaryAmount))
        {
            record.SalaryAmount = _encryptionService.Decrypt(record.SalaryAmount);
        }
    }
}

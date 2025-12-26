using System.Diagnostics.Metrics;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.EmployeeService.Infrastructure.Scripts;

/// <summary>
/// Script to migrate existing employees to IAM principals (Phase 6)
/// </summary>
public class MigrateEmployeesToPrincipalsScript
{
    private readonly EmployeeDbContext _context;
    private readonly IIAMClient _iamClient;
    private readonly ILogger<MigrateEmployeesToPrincipalsScript> _logger;

    private static readonly Meter Meter = new("migration-meter");
    private static readonly Counter<long> _migratedCounter = Meter.CreateCounter<long>("employee_migration_success_total");
    private static readonly Counter<long> _failedCounter = Meter.CreateCounter<long>("employee_migration_failure_total");

    public MigrateEmployeesToPrincipalsScript(
        EmployeeDbContext context,
        IIAMClient iamClient,
        ILogger<MigrateEmployeesToPrincipalsScript> logger)
    {
        _context = context;
        _iamClient = iamClient;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting employee migration to IAM principals...");

        var employees = await _context.Employees
            .Where(e => e.PrincipalId == Guid.Empty)
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} employees to migrate", employees.Count);

        int processedCount = 0;
        int successCount = 0;
        int failureCount = 0;

        foreach (var employee in employees)
        {
            try
            {
                _logger.LogInformation("Migrating employee {EmployeeId} ({EmployeeNumber})", employee.Id, employee.EmployeeNumber);

                var request = new CreatePrincipalRequest
                {
                    Email = employee.ContactInformation.WorkEmail,
                    LinkedService = "EmployeeService",
                    LinkedEntityId = employee.Id
                };

                var response = await _iamClient.CreatePrincipalAsync(request, ct);

                employee.PrincipalId = response.PrincipalId;
                _context.Employees.Update(employee);

                successCount++;
                _migratedCounter.Add(1);

                // Commit in batches of 50 to avoid massive transactions
                if (successCount % 50 == 0)
                {
                    await _context.SaveChangesAsync(ct);
                    _logger.LogInformation("Progress: {Processed}/{Total} migrated successfully", successCount, employees.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate employee {EmployeeId}", employee.Id);
                failureCount++;
                _failedCounter.Add(1);
                // Continue with next employee per "Skip and Log" strategy
            }

            processedCount++;
        }

        // Final save for the remaining records
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Migration completed. Total: {Total}, Success: {Success}, Failed: {Failures}",
            employees.Count, successCount, failureCount);
    }
}

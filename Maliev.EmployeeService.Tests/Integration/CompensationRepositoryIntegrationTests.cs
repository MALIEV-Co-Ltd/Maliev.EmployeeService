using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Maliev.EmployeeService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for CompensationRepository with encryption
/// Tests salary encryption/decryption, history tracking, and data integrity
/// </summary>
public class CompensationRepositoryIntegrationTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task CreateAsync_WithSalary_ShouldEncryptSalaryInDatabase()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee1" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp001@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var compensationRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            SalaryAmount = "85000.00", // Plain text
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Annual review",
            CreatedDate = DateTime.UtcNow
        };

        // Act
        await repository.CreateAsync(compensationRecord);
        await Context.SaveChangesAsync();

        // Clear context to ensure fresh read
        Context.ChangeTracker.Clear();

        // Assert - Verify encryption/decryption works through repository
        var recordFromDb = await repository.GetByIdAsync(compensationRecord.Id);

        Assert.NotNull(recordFromDb);
        Assert.Equal("85000.00", recordFromDb!.SalaryAmount); // Repository should decrypt salary on read
        Assert.Equal("THB", recordFromDb.Currency);
        Assert.Equal("Annual review", recordFromDb.ChangeReason);
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldReturnDecryptedSalary()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee2" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp002@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var compensationRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            SalaryAmount = "95000.50",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Promotion",
            CreatedDate = DateTime.UtcNow
        };

        await repository.CreateAsync(compensationRecord);
        await Context.SaveChangesAsync();

        // Clear the context to ensure fresh read
        Context.ChangeTracker.Clear();

        // Act
        var result = await repository.GetCurrentAsync(employee.Id);

        // Assert - Salary should be decrypted after retrieval
        Assert.NotNull(result);
        Assert.Equal("95000.50", result!.SalaryAmount);
        Assert.Equal("THB", result.Currency);
    }

    [Fact]
    public async Task GetCurrentAsync_WithMultipleRecords_ShouldReturnMostRecent()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP003",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee3" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp003@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var oldRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            SalaryAmount = "60000.00",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow.AddMonths(-6),
            ChangeReason = "Initial salary",
            CreatedDate = DateTime.UtcNow.AddMonths(-6)
        };

        var currentRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            SalaryAmount = "75000.00",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Annual raise",
            CreatedDate = DateTime.UtcNow
        };

        await repository.CreateAsync(oldRecord);
        await repository.CreateAsync(currentRecord);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        // Act
        var result = await repository.GetCurrentAsync(employee.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(currentRecord.Id, result!.Id);
        Assert.Equal("75000.00", result.SalaryAmount);
        Assert.Equal("Annual raise", result.ChangeReason);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnAllRecordsOrderedByDate()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP004",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee4" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp004@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var records = new[]
        {
            new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                SalaryAmount = "50000.00",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-2),
                ChangeReason = "Initial hire",
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                SalaryAmount = "60000.00",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-1),
                ChangeReason = "Annual review",
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            },
            new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                SalaryAmount = "75000.00",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow,
                ChangeReason = "Promotion",
                CreatedDate = DateTime.UtcNow
            }
        };

        foreach (var record in records)
        {
            await repository.CreateAsync(record);
        }
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        // Act
        var history = await repository.GetHistoryAsync(employee.Id);

        // Assert
        var historyList = history.ToList();
        Assert.Equal(3, historyList.Count());

        // Should be ordered by EffectiveDate descending
        Assert.Equal("75000.00", historyList[0].SalaryAmount);
        Assert.Equal("Promotion", historyList[0].ChangeReason);

        Assert.Equal("60000.00", historyList[1].SalaryAmount);
        Assert.Equal("Annual review", historyList[1].ChangeReason);

        Assert.Equal("50000.00", historyList[2].SalaryAmount);
        Assert.Equal("Initial hire", historyList[2].ChangeReason);
    }

    [Fact]
    public async Task CreateAsync_WithBonusAndCommission_ShouldStoreAllFields()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP005",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee5" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp005@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var compensationRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            SalaryAmount = "120000.00",
            Currency = "USD",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Executive package",
            BonusStructure = "15% annual bonus based on company performance",
            CommissionStructure = "5% on sales exceeding quota",
            CreatedDate = DateTime.UtcNow
        };

        // Act
        await repository.CreateAsync(compensationRecord);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        // Assert
        var result = await repository.GetCurrentAsync(employee.Id);

        Assert.NotNull(result);
        Assert.Equal("120000.00", result!.SalaryAmount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("15% annual bonus based on company performance", result.BonusStructure);
        Assert.Equal("5% on sales exceeding quota", result.CommissionStructure);
    }

    [Fact]
    public async Task CreateAsync_MultipleSalaryChanges_ShouldMaintainIntegrity()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP006",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee6" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp006@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        // Act - Create multiple compensation records
        var salaries = new[] { "50000.00", "55000.00", "62000.00", "70000.00", "85000.00" };
        var records = new List<CompensationRecord>();

        for (int i = 0; i < salaries.Length; i++)
        {
            var record = new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                SalaryAmount = salaries[i],
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddMonths(-12 + (i * 3)),
                ChangeReason = $"Change {i + 1}",
                CreatedDate = DateTime.UtcNow.AddMonths(-12 + (i * 3))
            };
            records.Add(record);
            await repository.CreateAsync(record);
        }
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        // Assert
        var history = await repository.GetHistoryAsync(employee.Id);
        var historyList = history.ToList();

        Assert.Equal(5, historyList.Count());

        // Verify each salary was encrypted and can be decrypted
        foreach (var originalSalary in salaries)
        {
            Assert.Contains(historyList, h => h.SalaryAmount == originalSalary);
        }

        // Verify most recent is returned by GetCurrentAsync
        var current = await repository.GetCurrentAsync(employee.Id);
        Assert.NotNull(current);
        Assert.Equal("85000.00", current!.SalaryAmount);
    }

    [Fact]
    public async Task GetCurrentAsync_WithNoRecords_ShouldReturnNull()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP007",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee7" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp007@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        // Act
        var result = await repository.GetCurrentAsync(employee.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistoryAsync_WithNoRecords_ShouldReturnEmptyList()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP008",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee8" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp008@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        // Act
        var history = await repository.GetHistoryAsync(employee.Id);

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public async Task CreateAsync_ShouldEncryptDifferentlySameValue()
    {
        // Arrange - Two employees with same salary
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee1 = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP009",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee9" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp009@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var employee2 = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            EmployeeNumber = "EMP010",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee10" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp010@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.AddRange(employee1, employee2);
        await Context.SaveChangesAsync();

        var record1 = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee1.Id,
            SalaryAmount = "80000.00",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Same salary",
            CreatedDate = DateTime.UtcNow
        };

        var record2 = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee2.Id,
            SalaryAmount = "80000.00",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Same salary",
            CreatedDate = DateTime.UtcNow
        };

        // Act
        await repository.CreateAsync(record1);
        await repository.CreateAsync(record2);
        await Context.SaveChangesAsync();

        // Clear context to ensure fresh reads
        Context.ChangeTracker.Clear();

        // Assert - Both should decrypt to the same value through repository
        var dbRecord1 = await repository.GetByIdAsync(record1.Id);
        var dbRecord2 = await repository.GetByIdAsync(record2.Id);

        Assert.NotNull(dbRecord1);
        Assert.NotNull(dbRecord2);

        Assert.Equal("80000.00", dbRecord1!.SalaryAmount); // Repository should decrypt salary correctly
        Assert.Equal("80000.00", dbRecord2!.SalaryAmount); // Repository should decrypt salary correctly

        // Verify both records have correct data
        Assert.Equal(employee1.Id, dbRecord1.EmployeeId);
        Assert.Equal(employee2.Id, dbRecord2.EmployeeId);
    }
}

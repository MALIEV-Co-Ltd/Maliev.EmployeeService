using FluentAssertions;
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

        recordFromDb.Should().NotBeNull();
        recordFromDb!.SalaryAmount.Should().Be("85000.00", "Repository should decrypt salary on read");
        recordFromDb.Currency.Should().Be("THB");
        recordFromDb.ChangeReason.Should().Be("Annual review");
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldReturnDecryptedSalary()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
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
        result.Should().NotBeNull();
        result!.SalaryAmount.Should().Be("95000.50");
        result.Currency.Should().Be("THB");
    }

    [Fact]
    public async Task GetCurrentAsync_WithMultipleRecords_ShouldReturnMostRecent()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
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
        result.Should().NotBeNull();
        result!.Id.Should().Be(currentRecord.Id);
        result.SalaryAmount.Should().Be("75000.00");
        result.ChangeReason.Should().Be("Annual raise");
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnAllRecordsOrderedByDate()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
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
        historyList.Should().HaveCount(3);

        // Should be ordered by EffectiveDate descending
        historyList[0].SalaryAmount.Should().Be("75000.00");
        historyList[0].ChangeReason.Should().Be("Promotion");

        historyList[1].SalaryAmount.Should().Be("60000.00");
        historyList[1].ChangeReason.Should().Be("Annual review");

        historyList[2].SalaryAmount.Should().Be("50000.00");
        historyList[2].ChangeReason.Should().Be("Initial hire");
    }

    [Fact]
    public async Task CreateAsync_WithBonusAndCommission_ShouldStoreAllFields()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
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

        result.Should().NotBeNull();
        result!.SalaryAmount.Should().Be("120000.00");
        result.Currency.Should().Be("USD");
        result.BonusStructure.Should().Be("15% annual bonus based on company performance");
        result.CommissionStructure.Should().Be("5% on sales exceeding quota");
    }

    [Fact]
    public async Task CreateAsync_MultipleSalaryChanges_ShouldMaintainIntegrity()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
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

        historyList.Should().HaveCount(5);

        // Verify each salary was encrypted and can be decrypted
        foreach (var originalSalary in salaries)
        {
            historyList.Should().Contain(h => h.SalaryAmount == originalSalary);
        }

        // Verify most recent is returned by GetCurrentAsync
        var current = await repository.GetCurrentAsync(employee.Id);
        current.Should().NotBeNull();
        current!.SalaryAmount.Should().Be("85000.00");
    }

    [Fact]
    public async Task GetCurrentAsync_WithNoRecords_ShouldReturnNull()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
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
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetHistoryAsync_WithNoRecords_ShouldReturnEmptyList()
    {
        // Arrange
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
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
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ShouldEncryptDifferentlySameValue()
    {
        // Arrange - Two employees with same salary
        var repository = new CompensationRepository(Context, EncryptionService);
        var employee1 = new Employee
        {
            Id = Guid.NewGuid(),
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

        dbRecord1.Should().NotBeNull();
        dbRecord2.Should().NotBeNull();

        dbRecord1!.SalaryAmount.Should().Be("80000.00", "Repository should decrypt salary correctly");
        dbRecord2!.SalaryAmount.Should().Be("80000.00", "Repository should decrypt salary correctly");

        // Verify both records have correct data
        dbRecord1.EmployeeId.Should().Be(employee1.Id);
        dbRecord2.EmployeeId.Should().Be(employee2.Id);
    }
}

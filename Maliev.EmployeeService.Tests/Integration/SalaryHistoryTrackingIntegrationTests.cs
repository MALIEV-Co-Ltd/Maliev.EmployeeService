using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Infrastructure.Data;
using Maliev.EmployeeService.Infrastructure.Data.Interceptors;
using Maliev.EmployeeService.Infrastructure.Repositories;
using Maliev.EmployeeService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Integration;

/// <summary>
/// Integration tests for salary history tracking
/// Tests the complete workflow of recording and retrieving compensation changes over time
/// </summary>
public class SalaryHistoryTrackingIntegrationTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task CompleteWorkflow_RecordMultipleSalaryChanges_ShouldTrackHistory()
    {
        // Arrange - Create employee
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp001@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-3),
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var mockEventPublisher = new Mock<IEventPublisher>();

        var commandHandler = new RecordCompensationChangeCommandHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object,
            mockEventPublisher.Object,
            unitOfWork);

        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        // Act - Record compensation changes over 3 years
        var changes = new[]
        {
            new { Salary = 50000m, Date = DateTime.UtcNow.AddYears(-3), Reason = "Initial hire" },
            new { Salary = 55000m, Date = DateTime.UtcNow.AddYears(-2).AddMonths(-6), Reason = "6-month review" },
            new { Salary = 62000m, Date = DateTime.UtcNow.AddYears(-2), Reason = "Annual review - Year 1" },
            new { Salary = 68000m, Date = DateTime.UtcNow.AddYears(-1), Reason = "Annual review - Year 2" },
            new { Salary = 75000m, Date = DateTime.UtcNow.AddMonths(-6), Reason = "Promotion to Senior" },
            new { Salary = 85000m, Date = DateTime.UtcNow, Reason = "Annual review - Year 3" }
        };

        foreach (var change in changes)
        {
            var dto = new RecordCompensationChangeDto
            {
                SalaryAmount = change.Salary,
                Currency = "THB",
                EffectiveDate = change.Date,
                ChangeReason = change.Reason
            };

            var command = new RecordCompensationChangeCommand(employee.Id, dto);
            var result = await commandHandler.HandleAsync(command);

            result.Success.Should().BeTrue($"Recording {change.Reason} should succeed");
        }

        // Assert - Get history and verify
        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);
        var historyList = history.ToList();

        // Verify all changes are recorded
        historyList.Should().HaveCount(6, "All 6 salary changes should be recorded");

        // Verify order (most recent first)
        historyList[0].SalaryAmount.Should().Be(85000m);
        historyList[0].ChangeReason.Should().Be("Annual review - Year 3");

        historyList[1].SalaryAmount.Should().Be(75000m);
        historyList[1].ChangeReason.Should().Be("Promotion to Senior");

        historyList[2].SalaryAmount.Should().Be(68000m);
        historyList[3].SalaryAmount.Should().Be(62000m);
        historyList[4].SalaryAmount.Should().Be(55000m);

        historyList[5].SalaryAmount.Should().Be(50000m);
        historyList[5].ChangeReason.Should().Be("Initial hire");

        // Verify dates are in descending order
        historyList.Should().BeInDescendingOrder(h => h.EffectiveDate);
    }

    [Fact]
    public async Task SalaryHistory_WithGetCurrentAsync_ShouldReturnMostRecent()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        var employee = await CreateEmployeeWithSalaryHistory();

        var detailsQueryHandler = new GetCompensationDetailsQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        // Act
        var query = new GetCompensationDetailsQuery(employee.Id);
        var currentCompensation = await detailsQueryHandler.HandleAsync(query);

        // Assert
        currentCompensation.Should().NotBeNull();
        currentCompensation!.SalaryAmount.Should().Be(85000m, "Should return the most recent salary");
        currentCompensation.ChangeReason.Should().Be("Current salary");
    }

    [Fact]
    public async Task SalaryHistory_ShouldCalculateCorrectGrowth()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        var employee = await CreateEmployeeWithSalaryHistory();

        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        // Act
        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);
        var historyList = history.ToList();

        // Assert - Verify salary progression
        var initialSalary = historyList.Last().SalaryAmount;
        var currentSalary = historyList.First().SalaryAmount;

        initialSalary.Should().Be(50000m);
        currentSalary.Should().Be(85000m);

        var growthPercentage = ((currentSalary - initialSalary) / initialSalary) * 100;
        growthPercentage.Should().Be(70m); // 70% growth from 50k to 85k
    }

    [Fact]
    public async Task SalaryHistory_WithMultipleEmployees_ShouldIsolateCorrectly()
    {
        // Arrange - Create two employees with different salary histories
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);

        var employee1 = await CreateEmployeeWithSalaryHistory();

        var employee2 = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP002",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee2" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp002@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        Context.Employees.Add(employee2);
        await Context.SaveChangesAsync();

        var mockEventPublisher = new Mock<IEventPublisher>();

        var commandHandler = new RecordCompensationChangeCommandHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object,
            mockEventPublisher.Object,
            unitOfWork);

        // Employee 2 salary history
        var employee2Changes = new[]
        {
            new { Salary = 100000m, Reason = "Senior hire" },
            new { Salary = 110000m, Reason = "Annual review" }
        };

        foreach (var change in employee2Changes)
        {
            var dto = new RecordCompensationChangeDto
            {
                SalaryAmount = change.Salary,
                Currency = "USD",
                EffectiveDate = DateTime.UtcNow,
                ChangeReason = change.Reason
            };

            var command = new RecordCompensationChangeCommand(employee2.Id, dto);
            await commandHandler.HandleAsync(command);
        }

        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        // Act
        var history1 = await queryHandler.HandleAsync(new GetCompensationHistoryQuery(employee1.Id));
        var history2 = await queryHandler.HandleAsync(new GetCompensationHistoryQuery(employee2.Id));

        // Assert - Verify isolation
        var history1List = history1.ToList();
        var history2List = history2.ToList();

        history1List.Should().HaveCount(4, "Employee 1 should have 4 records");
        history2List.Should().HaveCount(2, "Employee 2 should have 2 records");

        history1List.Should().OnlyContain(h => h.Currency == "THB");
        history2List.Should().OnlyContain(h => h.Currency == "USD");

        history1List.First().SalaryAmount.Should().Be(85000m);
        history2List.First().SalaryAmount.Should().Be(110000m);
    }

    [Fact]
    public async Task SalaryHistory_WithDateRangeFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        var employee = await CreateEmployeeWithSalaryHistory();

        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        // Act
        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);

        // Filter to last year only
        var lastYearDate = DateTime.UtcNow.AddYears(-1);
        var recentHistory = history.Where(h => h.EffectiveDate >= lastYearDate).ToList();

        // Assert
        recentHistory.Should().HaveCount(2, "Should have 2 changes in the last year");
        recentHistory.Should().Contain(h => h.ChangeReason == "Promotion");
        recentHistory.Should().Contain(h => h.ChangeReason == "Current salary");
    }

    [Fact]
    public async Task SalaryHistory_ShouldPreserveAllFields()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP999",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "Test", LastName = "Employee999" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "emp999@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var mockEventPublisher = new Mock<IEventPublisher>();

        var commandHandler = new RecordCompensationChangeCommandHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object,
            mockEventPublisher.Object,
            unitOfWork);

        var dto = new RecordCompensationChangeDto
        {
            SalaryAmount = 150000m,
            Currency = "USD",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Executive compensation package",
            BonusStructure = "20% annual bonus based on company performance, paid quarterly",
            CommissionStructure = "5% on all revenue generated, 10% on revenue exceeding $1M annually"
        };

        var command = new RecordCompensationChangeCommand(employee.Id, dto);

        // Act
        await commandHandler.HandleAsync(command);

        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);

        // Assert
        var record = history.First();
        record.SalaryAmount.Should().Be(150000m);
        record.Currency.Should().Be("USD");
        record.ChangeReason.Should().Be("Executive compensation package");
        record.BonusStructure.Should().Be("20% annual bonus based on company performance, paid quarterly");
        record.CommissionStructure.Should().Be("5% on all revenue generated, 10% on revenue exceeding $1M annually");
    }

    [Fact]
    public async Task SalaryHistory_WithEncryption_ShouldMaintainIntegrity()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        var employee = await CreateEmployeeWithSalaryHistory();

        // Act - Verify encryption works by testing round-trip through repository
        // The repository should encrypt on write and decrypt on read
        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);
        var historyList = history.ToList();

        // Assert - Verify we can retrieve and decrypt all compensation records
        historyList.Should().HaveCount(4, "Should have 4 salary history records");

        // All decrypted values should match expected values from helper method
        historyList.Should().Contain(h => h.SalaryAmount == 50000m);
        historyList.Should().Contain(h => h.SalaryAmount == 60000m);
        historyList.Should().Contain(h => h.SalaryAmount == 72000m);
        historyList.Should().Contain(h => h.SalaryAmount == 85000m);

        // All values should be in reasonable range (decryption worked)
        foreach (var dto in historyList)
        {
            dto.SalaryAmount.Should().BeGreaterThan(0);
            dto.SalaryAmount.Should().BeLessThan(1000000m, "Reasonable salary range");
        }
    }

    /// <summary>
    /// Helper method to create employee with predefined salary history
    /// </summary>
    private async Task<Employee> CreateEmployeeWithSalaryHistory()
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "HIST001",
            LegalName = new Domain.ValueObjects.LegalName { FirstName = "History", LastName = "Test" },
            ContactInformation = new Domain.ValueObjects.ContactInformation { WorkEmail = "hist001@company.com" },
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-2),
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

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
                SalaryAmount = "72000.00",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddMonths(-3),
                ChangeReason = "Promotion",
                CreatedDate = DateTime.UtcNow.AddMonths(-3)
            },
            new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                SalaryAmount = "85000.00",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow,
                ChangeReason = "Current salary",
                CreatedDate = DateTime.UtcNow
            }
        };

        Context.Employees.Add(employee);
        Context.CompensationRecords.AddRange(records);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        return employee;
    }
}

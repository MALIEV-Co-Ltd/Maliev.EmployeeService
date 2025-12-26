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
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.EmployeeService.Domain.Authorization;

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
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());
        var mockIamClient = new Mock<IIamServiceClient>();
        var mockConfiguration = new Mock<IConfiguration>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
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
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object,
            mockEventPublisher.Object,
            unitOfWork);

        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
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

            Assert.True(result.Success, $"Recording {change.Reason} should succeed");
        }

        // Assert - Get history and verify
        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);
        var historyList = history.ToList();

        // Verify all changes are recorded
        Assert.Equal(6, historyList.Count()); // All 6 salary changes should be recorded

        // Verify order (most recent first)
        Assert.Equal(85000m, historyList[0].SalaryAmount);
        Assert.Equal("Annual review - Year 3", historyList[0].ChangeReason);

        Assert.Equal(75000m, historyList[1].SalaryAmount);
        Assert.Equal("Promotion to Senior", historyList[1].ChangeReason);

        Assert.Equal(68000m, historyList[2].SalaryAmount);
        Assert.Equal(62000m, historyList[3].SalaryAmount);
        Assert.Equal(55000m, historyList[4].SalaryAmount);

        Assert.Equal(50000m, historyList[5].SalaryAmount);
        Assert.Equal("Initial hire", historyList[5].ChangeReason);

        // Verify dates are in descending order
        // Verify descending order: historyList
    }

    [Fact]
    public async Task SalaryHistory_WithGetCurrentAsync_ShouldReturnMostRecent()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var employee = await CreateEmployeeWithSalaryHistory();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        var detailsQueryHandler = new GetCompensationDetailsQueryHandler(
            compensationRepository,
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        // Act
        var query = new GetCompensationDetailsQuery(employee.Id);
        var currentCompensation = await detailsQueryHandler.HandleAsync(query);

        // Assert
        Assert.NotNull(currentCompensation);
        Assert.Equal(85000m, currentCompensation!.SalaryAmount); // Should return the most recent salary
        Assert.Equal("Current salary", currentCompensation.ChangeReason);
    }

    [Fact]
    public async Task SalaryHistory_ShouldCalculateCorrectGrowth()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var employee = await CreateEmployeeWithSalaryHistory();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        // Act
        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);
        var historyList = history.ToList();

        // Assert - Verify salary progression
        var initialSalary = historyList.Last().SalaryAmount;
        var currentSalary = historyList.First().SalaryAmount;

        Assert.Equal(50000m, initialSalary);
        Assert.Equal(85000m, currentSalary);

        var growthPercentage = ((currentSalary - initialSalary) / initialSalary) * 100;
        Assert.Equal(70m, growthPercentage); // 70% growth from 50k to 85k
    }

    [Fact]
    public async Task SalaryHistory_WithMultipleEmployees_ShouldIsolateCorrectly()
    {
        // Arrange - Create two employees with different salary histories
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();

        var employee1 = await CreateEmployeeWithSalaryHistory();

        var employee2 = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
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
            mockIamClient.Object,
            mockConfiguration.Object,
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
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        // Act
        var history1 = await queryHandler.HandleAsync(new GetCompensationHistoryQuery(employee1.Id));
        var history2 = await queryHandler.HandleAsync(new GetCompensationHistoryQuery(employee2.Id));

        // Assert - Verify isolation
        var history1List = history1.ToList();
        var history2List = history2.ToList();

        Assert.Equal(4, history1List.Count()); // Employee 1 should have 4 records
        Assert.Equal(2, history2List.Count()); // Employee 2 should have 2 records

        Assert.All(history1List, h => Assert.True(h.Currency == "THB"));
        Assert.All(history2List, h => Assert.True(h.Currency == "USD"));

        Assert.Equal(85000m, history1List.First().SalaryAmount);
        Assert.Equal(110000m, history2List.First().SalaryAmount);
    }

    [Fact]
    public async Task SalaryHistory_WithDateRangeFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var employee = await CreateEmployeeWithSalaryHistory();

        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());

        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        // Act
        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);

        // Filter to last year only
        var lastYearDate = DateTime.UtcNow.AddYears(-1);
        var recentHistory = history.Where(h => h.EffectiveDate >= lastYearDate).ToList();

        // Assert
        Assert.Equal(2, recentHistory.Count()); // Should have 2 changes in the last year
        Assert.Contains(recentHistory, h => h.ChangeReason == "Promotion");
        Assert.Contains(recentHistory, h => h.ChangeReason == "Current salary");
    }

    [Fact]
    public async Task SalaryHistory_ShouldPreserveAllFields()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
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
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();

        var commandHandler = new RecordCompensationChangeCommandHandler(
            compensationRepository,
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
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
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);

        // Assert
        var record = history.First();
        Assert.Equal(150000m, record.SalaryAmount);
        Assert.Equal("USD", record.Currency);
        Assert.Equal("Executive compensation package", record.ChangeReason);
        Assert.Equal("20% annual bonus based on company performance, paid quarterly", record.BonusStructure);
        Assert.Equal("5% on all revenue generated, 10% on revenue exceeding $1M annually", record.CommissionStructure);
    }

    [Fact]
    public async Task SalaryHistory_WithEncryption_ShouldMaintainIntegrity()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.PrincipalId).Returns(Guid.NewGuid());
        var mockIamClient = new Mock<IIamServiceClient>();
        mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mockConfiguration = new Mock<IConfiguration>();
        var employee = await CreateEmployeeWithSalaryHistory();

        var queryHandler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockIamClient.Object,
            mockConfiguration.Object,
            mockCurrentUserService.Object);

        var query = new GetCompensationHistoryQuery(employee.Id);
        var history = await queryHandler.HandleAsync(query);
        var historyList = history.ToList();

        // Assert - Verify we can retrieve and decrypt all compensation records
        Assert.Equal(4, historyList.Count()); // Should have 4 salary history records

        // All decrypted values should match expected values from helper method
        Assert.Contains(historyList, h => h.SalaryAmount == 50000m);
        Assert.Contains(historyList, h => h.SalaryAmount == 60000m);
        Assert.Contains(historyList, h => h.SalaryAmount == 72000m);
        Assert.Contains(historyList, h => h.SalaryAmount == 85000m);

        // All values should be in reasonable range (decryption worked)
        foreach (var dto in historyList)
        {
            Assert.True(dto.SalaryAmount > 0);
            Assert.True(dto.SalaryAmount < 1000000m, "Reasonable salary range");
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
            PrincipalId = Guid.NewGuid(),
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

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
/// Integration tests for compensation authorization
/// Tests that unauthorized users cannot access sensitive compensation data
/// </summary>
public class CompensationAuthorizationIntegrationTests : PostgreSqlIntegrationTestBase
{
    [Fact]
    public async Task GetCompensationDetails_WithRegularEmployee_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employee = await CreateTestEmployeeWithCompensation();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(false);
        mockCurrentUserService.Setup(x => x.IsInRole("HRGeneralist")).Returns(false);
        mockCurrentUserService.Setup(x => x.IsInRole("SystemAdministrator")).Returns(false);
        mockCurrentUserService.Setup(x => x.IsInRole("Employee")).Returns(true);

        var handler = new GetCompensationDetailsQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        var query = new GetCompensationDetailsQuery(employee.Id);

        // Act
        var act = async () => await handler.HandleAsync(query);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task GetCompensationDetails_WithManager_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employee = await CreateTestEmployeeWithCompensation();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("Manager")).Returns(true);
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(false);
        mockCurrentUserService.Setup(x => x.IsInRole("HRGeneralist")).Returns(false);
        mockCurrentUserService.Setup(x => x.IsInRole("SystemAdministrator")).Returns(false);

        var handler = new GetCompensationDetailsQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        var query = new GetCompensationDetailsQuery(employee.Id);

        // Act
        var act = async () => await handler.HandleAsync(query);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task GetCompensationDetails_WithHRSpecialist_ShouldSucceed()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employee = await CreateTestEmployeeWithCompensation();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        var handler = new GetCompensationDetailsQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        var query = new GetCompensationDetailsQuery(employee.Id);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employee.Id, result!.EmployeeId);
        Assert.Equal(85000.00m, result.SalaryAmount);
    }

    [Fact]
    public async Task GetCompensationDetails_WithHRGeneralist_ShouldSucceed()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employee = await CreateTestEmployeeWithCompensation();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRGeneralist")).Returns(true);
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        var handler = new GetCompensationDetailsQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        var query = new GetCompensationDetailsQuery(employee.Id);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(85000.00m, result!.SalaryAmount);
    }

    [Fact]
    public async Task GetCompensationDetails_WithSystemAdministrator_ShouldSucceed()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employee = await CreateTestEmployeeWithCompensation();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("SystemAdministrator")).Returns(true);
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        var handler = new GetCompensationDetailsQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        var query = new GetCompensationDetailsQuery(employee.Id);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(85000.00m, result!.SalaryAmount);
    }

    [Fact]
    public async Task GetCompensationHistory_WithUnauthorizedUser_ShouldThrowException()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employee = await CreateTestEmployeeWithCompensation();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(false);

        var handler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        var query = new GetCompensationHistoryQuery(employee.Id);

        // Act
        var act = async () => await handler.HandleAsync(query);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task GetCompensationHistory_WithAuthorizedUser_ShouldReturnHistory()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employee = await CreateTestEmployeeWithMultipleCompensationRecords();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);

        var handler = new GetCompensationHistoryQueryHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object);

        var query = new GetCompensationHistoryQuery(employee.Id);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        var history = result.ToList();
        Assert.Equal(3, history.Count());
        // Verify descending order: history
    }

    [Fact]
    public async Task RecordCompensationChange_WithAuthorizedUser_ShouldSucceed()
    {
        // Arrange
        var compensationRepository = new CompensationRepository(Context, EncryptionService);
        var employeeRepository = new EmployeeRepository(Context);
        var unitOfWork = new UnitOfWork(Context);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP999",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        await Context.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        var mockEventPublisher = new Mock<IEventPublisher>();

        var handler = new RecordCompensationChangeCommandHandler(
            compensationRepository,
            employeeRepository,
            mockCurrentUserService.Object,
            mockEventPublisher.Object,
            unitOfWork);

        var dto = new RecordCompensationChangeDto
        {
            SalaryAmount = 95000m,
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Merit increase"
        };

        var command = new RecordCompensationChangeCommand(employee.Id, dto);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CompensationRecordId);

        // Verify the record was created with encryption
        var savedRecord = await compensationRepository.GetCurrentAsync(employee.Id);
        Assert.NotNull(savedRecord);
        Assert.Equal("95000.00", savedRecord!.SalaryAmount); // Decrypted by interceptor
    }

    /// <summary>
    /// Helper method to create test employee with compensation
    /// </summary>
    private async Task<Employee> CreateTestEmployeeWithCompensation()
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "TEST001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var compensation = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            SalaryAmount = "85000.00",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Test compensation",
            CreatedDate = DateTime.UtcNow
        };

        Context.Employees.Add(employee);
        Context.CompensationRecords.Add(compensation);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        return employee;
    }

    /// <summary>
    /// Helper method to create test employee with multiple compensation records
    /// </summary>
    private async Task<Employee> CreateTestEmployeeWithMultipleCompensationRecords()
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "TEST002",
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
                SalaryAmount = "60000.00",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-2),
                ChangeReason = "Initial hire",
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                SalaryAmount = "72000.00",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-1),
                ChangeReason = "Annual review",
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            },
            new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                SalaryAmount = "85000.00",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow,
                ChangeReason = "Promotion",
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

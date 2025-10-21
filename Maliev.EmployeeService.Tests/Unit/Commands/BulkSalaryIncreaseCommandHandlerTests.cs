using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for BulkSalaryIncreaseCommandHandler
/// Tests bulk salary increase with preview mode and execution mode
/// </summary>
public class BulkSalaryIncreaseCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<ICompensationRepository> _mockCompensationRepository;
    private readonly Mock<IBulkJobRepository> _mockBulkJobRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly BulkSalaryIncreaseCommandHandler _handler;

    public BulkSalaryIncreaseCommandHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockCompensationRepository = new Mock<ICompensationRepository>();
        _mockBulkJobRepository = new Mock<IBulkJobRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new BulkSalaryIncreaseCommandHandler(
            _mockEmployeeRepository.Object,
            _mockCompensationRepository.Object,
            _mockBulkJobRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_WithPreviewMode_ShouldReturnPreviewWithoutPersisting()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var legalName = new LegalName { FirstName = "John", LastName = "Doe" };

        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                DepartmentId = department.Id,
                Department = department,
                LegalName = legalName,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                DepartmentId = department.Id,
                Department = department,
                LegalName = legalName,
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        // Mock compensation records
        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employees[0].Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[0].Id,
                SalaryAmount = "50000",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-2),
                ChangeReason = "Initial Salary",
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            });

        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employees[1].Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[1].Id,
                SalaryAmount = "60000",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-1),
                ChangeReason = "Initial Salary",
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            });

        var command = new BulkSalaryIncreaseCommand
        {
            PercentageIncrease = 10, // 10% increase
            Reason = "Annual Review 2024",
            EffectiveDate = DateTime.UtcNow.AddDays(30),
            PreviewOnly = true,
            InitiatedByUserId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.IsPreview.Should().BeTrue();
        result.TotalEmployees.Should().Be(2);
        result.Changes.Should().HaveCount(2);
        result.JobId.Should().BeNull(); // No job created in preview mode

        // Verify EMP001: 50000 + 10% = 55000
        var emp001Change = result.Changes.First(c => c.EmployeeNumber == "EMP001");
        emp001Change.CurrentSalary.Should().Be(50000);
        emp001Change.NewSalary.Should().Be(55000);
        emp001Change.IncreaseAmount.Should().Be(5000);
        emp001Change.PercentageIncrease.Should().Be(10);

        // Verify EMP002: 60000 + 10% = 66000
        var emp002Change = result.Changes.First(c => c.EmployeeNumber == "EMP002");
        emp002Change.CurrentSalary.Should().Be(60000);
        emp002Change.NewSalary.Should().Be(66000);
        emp002Change.IncreaseAmount.Should().Be(6000);

        // Total increase cost = 5000 + 6000 = 11000
        result.TotalIncreaseCost.Should().Be(11000);

        // Verify no persistence
        _mockCompensationRepository.Verify(x => x.AddAsync(It.IsAny<CompensationRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockBulkJobRepository.Verify(x => x.AddAsync(It.IsAny<BulkJob>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithExecutionMode_ShouldCreateBulkJobAndPersistChanges()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var legalName = new LegalName { FirstName = "John", LastName = "Doe" };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow.AddYears(-2),
            DepartmentId = department.Id,
            Department = department,
            LegalName = legalName,
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { employee });

        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                SalaryAmount = "100000",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-2),
                ChangeReason = "Initial Salary",
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            });

        _mockBulkJobRepository.Setup(x => x.AddAsync(It.IsAny<BulkJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCompensationRepository.Setup(x => x.AddAsync(It.IsAny<CompensationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new BulkSalaryIncreaseCommand
        {
            PercentageIncrease = 5, // 5% increase
            Reason = "Market Adjustment",
            EffectiveDate = DateTime.UtcNow.AddDays(15),
            PreviewOnly = false, // Execute mode
            InitiatedByUserId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.IsPreview.Should().BeFalse();
        result.JobId.Should().NotBeNull();
        result.TotalEmployees.Should().Be(1);
        result.TotalIncreaseCost.Should().Be(5000); // 100000 * 0.05 = 5000

        // Verify bulk job was created
        _mockBulkJobRepository.Verify(x => x.AddAsync(
            It.Is<BulkJob>(j =>
                j.JobType == "BulkSalaryIncrease" &&
                j.TotalRecords == 1 &&
                j.InitiatedByUserId == command.InitiatedByUserId),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify compensation record was created
        _mockCompensationRepository.Verify(x => x.AddAsync(
            It.Is<CompensationRecord>(c =>
                c.EmployeeId == employee.Id &&
                c.SalaryAmount == "105000.00" && // 100000 + 5% = 105000, formatted as "105000.00"
                c.ChangeReason == "Market Adjustment"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WithDepartmentFilter_ShouldOnlyProcessDepartmentEmployees()
    {
        // Arrange
        var engineeringDept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var salesDept = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Sales",
            HeadcountLimit = 30,
            CreatedDate = DateTime.UtcNow.AddYears(-3)
        };

        var legalName = new LegalName { FirstName = "John", LastName = "Doe" };

        var employees = new List<Employee>
        {
            // Engineering
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                DepartmentId = engineeringDept.Id,
                Department = engineeringDept,
                LegalName = legalName,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            // Sales - should be excluded
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-1),
                DepartmentId = salesDept.Id,
                Department = salesDept,
                LegalName = legalName,
                CreatedDate = DateTime.UtcNow.AddYears(-1)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employees[0].Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[0].Id,
                SalaryAmount = "50000",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-2),
                ChangeReason = "Initial Salary",
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            });

        var command = new BulkSalaryIncreaseCommand
        {
            DepartmentId = engineeringDept.Id, // Filter by Engineering
            PercentageIncrease = 10,
            Reason = "Annual Review",
            EffectiveDate = DateTime.UtcNow.AddDays(30),
            PreviewOnly = true,
            InitiatedByUserId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.TotalEmployees.Should().Be(1); // Only Engineering employee
        result.Changes.Should().HaveCount(1);
        result.Changes[0].EmployeeNumber.Should().Be("EMP001");

        // Verify we didn't query compensation for Sales employee
        _mockCompensationRepository.Verify(x => x.GetCurrentAsync(employees[1].Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPercentage_ShouldThrowException()
    {
        // Arrange
        var command = new BulkSalaryIncreaseCommand
        {
            PercentageIncrease = -5, // Invalid: negative percentage
            Reason = "Annual Review",
            EffectiveDate = DateTime.UtcNow.AddDays(30),
            PreviewOnly = true,
            InitiatedByUserId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_WithPercentageOver100_ShouldThrowException()
    {
        // Arrange
        var command = new BulkSalaryIncreaseCommand
        {
            PercentageIncrease = 150, // Invalid: over 100%
            Reason = "Annual Review",
            EffectiveDate = DateTime.UtcNow.AddDays(30),
            PreviewOnly = true,
            InitiatedByUserId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_WithNoActiveEmployees_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>()); // No employees

        var command = new BulkSalaryIncreaseCommand
        {
            PercentageIncrease = 10,
            Reason = "Annual Review",
            EffectiveDate = DateTime.UtcNow.AddDays(30),
            PreviewOnly = true,
            InitiatedByUserId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.TotalEmployees.Should().Be(0);
        result.Changes.Should().BeEmpty();
        result.TotalIncreaseCost.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_ShouldExcludeInactiveEmployees()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var legalName = new LegalName { FirstName = "John", LastName = "Doe" };

        var employees = new List<Employee>
        {
            // Active
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP001",
                EmploymentStatus = EmploymentStatus.Active,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-2),
                DepartmentId = department.Id,
                Department = department,
                LegalName = legalName,
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            },
            // Terminated - should be excluded
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = "EMP002",
                EmploymentStatus = EmploymentStatus.Terminated,
                EmploymentType = EmploymentType.FullTime,
                StartDate = DateTime.UtcNow.AddYears(-3),
                TerminationDate = DateTime.UtcNow.AddMonths(-1),
                DepartmentId = department.Id,
                Department = department,
                LegalName = legalName,
                CreatedDate = DateTime.UtcNow.AddYears(-3)
            }
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employees[0].Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[0].Id,
                SalaryAmount = "50000",
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-2),
                ChangeReason = "Initial Salary",
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            });

        var command = new BulkSalaryIncreaseCommand
        {
            PercentageIncrease = 10,
            Reason = "Annual Review",
            EffectiveDate = DateTime.UtcNow.AddDays(30),
            PreviewOnly = true,
            InitiatedByUserId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.TotalEmployees.Should().Be(1); // Only active employee
        result.Changes[0].EmployeeNumber.Should().Be("EMP001");
    }

    [Fact]
    public async Task HandleAsync_WithEmployeeWithoutCompensation_ShouldHandleGracefully()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var legalName = new LegalName { FirstName = "John", LastName = "Doe" };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow.AddDays(-1), // Very new employee
            DepartmentId = department.Id,
            Department = department,
            LegalName = legalName,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { employee });

        // No compensation record exists
        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompensationRecord?)null);

        var command = new BulkSalaryIncreaseCommand
        {
            PercentageIncrease = 10,
            Reason = "Annual Review",
            EffectiveDate = DateTime.UtcNow.AddDays(30),
            PreviewOnly = true,
            InitiatedByUserId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        // Employee without compensation should be skipped
        result.TotalEmployees.Should().Be(0);
        result.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateCorrectIncreaseAmounts()
    {
        // Arrange
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            HeadcountLimit = 50,
            CreatedDate = DateTime.UtcNow.AddYears(-5)
        };

        var legalName = new LegalName { FirstName = "John", LastName = "Doe" };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            EmploymentType = EmploymentType.FullTime,
            StartDate = DateTime.UtcNow.AddYears(-2),
            DepartmentId = department.Id,
            Department = department,
            LegalName = legalName,
            CreatedDate = DateTime.UtcNow.AddYears(-2)
        };

        _mockEmployeeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { employee });

        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompensationRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                SalaryAmount = "45678.90", // Test with decimal
                Currency = "THB",
                EffectiveDate = DateTime.UtcNow.AddYears(-2),
                ChangeReason = "Initial Salary",
                CreatedDate = DateTime.UtcNow.AddYears(-2)
            });

        var command = new BulkSalaryIncreaseCommand
        {
            PercentageIncrease = 7.5m, // 7.5% increase
            Reason = "Merit Increase",
            EffectiveDate = DateTime.UtcNow.AddDays(15),
            PreviewOnly = true,
            InitiatedByUserId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Changes.Should().HaveCount(1);

        var change = result.Changes[0];
        change.CurrentSalary.Should().Be(45678.90m);
        // 45678.90 * 0.075 = 3425.9175, rounded = 3425.92; newSalary = 45678.90 + 3425.92 = 49104.82
        change.NewSalary.Should().BeApproximately(49104.82m, 0.01m);
        change.IncreaseAmount.Should().BeApproximately(3425.92m, 0.01m);
        change.PercentageIncrease.Should().Be(7.5m);
    }
}

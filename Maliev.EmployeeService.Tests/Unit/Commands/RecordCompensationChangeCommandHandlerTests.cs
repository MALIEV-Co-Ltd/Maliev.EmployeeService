using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for RecordCompensationChangeCommandHandler
/// Tests salary validation, encryption handling, and audit logging
/// </summary>
public class RecordCompensationChangeCommandHandlerTests
{
    private readonly Mock<ICompensationRepository> _mockCompensationRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly RecordCompensationChangeCommandHandler _handler;

    public RecordCompensationChangeCommandHandlerTests()
    {
        _mockCompensationRepository = new Mock<ICompensationRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockEventPublisher = new Mock<IEventPublisher>();

        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        _handler = new RecordCompensationChangeCommandHandler(
            _mockCompensationRepository.Object,
            _mockEmployeeRepository.Object,
            _mockCurrentUserService.Object,
            _mockEventPublisher.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldCreateCompensationRecord()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var dto = new RecordCompensationChangeDto
        {
            SalaryAmount = 85000.00m,
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Annual salary review - performance bonus",
            BonusStructure = "10% annual performance bonus",
            CommissionStructure = "5% on sales over target"
        };

        var command = new RecordCompensationChangeCommand(employeeId, dto);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockCompensationRepository.Setup(x => x.CreateAsync(It.IsAny<CompensationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.CompensationRecordId.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();

        _mockCompensationRepository.Verify(x => x.CreateAsync(
            It.Is<CompensationRecord>(cr =>
                cr.EmployeeId == employeeId &&
                cr.SalaryAmount == "85000.00" && // Stored as string for encryption
                cr.Currency == "THB" &&
                cr.ChangeReason == "Annual salary review - performance bonus" &&
                cr.BonusStructure == "10% annual performance bonus" &&
                cr.CommissionStructure == "5% on sales over target"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithZeroSalary_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var dto = new RecordCompensationChangeDto
        {
            SalaryAmount = 0m,
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Test"
        };

        var command = new RecordCompensationChangeCommand(employeeId, dto);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.CompensationRecordId.Should().BeNull();
        result.ErrorMessage.Should().Contain("Salary amount must be greater than zero");

        _mockCompensationRepository.Verify(x => x.CreateAsync(It.IsAny<CompensationRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNegativeSalary_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var dto = new RecordCompensationChangeDto
        {
            SalaryAmount = -50000m,
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Test"
        };

        var command = new RecordCompensationChangeCommand(employeeId, dto);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.CompensationRecordId.Should().BeNull();
        result.ErrorMessage.Should().Contain("Salary amount must be greater than zero");

        _mockCompensationRepository.Verify(x => x.CreateAsync(It.IsAny<CompensationRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithLargeSalary_ShouldFormatCorrectly()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var dto = new RecordCompensationChangeDto
        {
            SalaryAmount = 1234567.89m,
            Currency = "USD",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Executive compensation"
        };

        var command = new RecordCompensationChangeCommand(employeeId, dto);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockCompensationRepository.Setup(x => x.CreateAsync(It.IsAny<CompensationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();

        _mockCompensationRepository.Verify(x => x.CreateAsync(
            It.Is<CompensationRecord>(cr =>
                cr.SalaryAmount == "1234567.89"), // Formatted to 2 decimal places
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithOptionalFieldsNull_ShouldSucceed()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        var dto = new RecordCompensationChangeDto
        {
            SalaryAmount = 50000m,
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Initial compensation",
            BonusStructure = null,
            CommissionStructure = null
        };

        var command = new RecordCompensationChangeCommand(employeeId, dto);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockCompensationRepository.Setup(x => x.CreateAsync(It.IsAny<CompensationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();

        _mockCompensationRepository.Verify(x => x.CreateAsync(
            It.Is<CompensationRecord>(cr =>
                cr.BonusStructure == null &&
                cr.CommissionStructure == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCurrentUserAsCreatedBy()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP006",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow.AddYears(-1),
            CreatedDate = DateTime.UtcNow.AddYears(-1)
        };

        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(currentUserId);

        var dto = new RecordCompensationChangeDto
        {
            SalaryAmount = 60000m,
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Promotion"
        };

        var command = new RecordCompensationChangeCommand(employeeId, dto);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockCompensationRepository.Setup(x => x.CreateAsync(It.IsAny<CompensationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();

        _mockCompensationRepository.Verify(x => x.CreateAsync(
            It.Is<CompensationRecord>(cr => cr.CreatedBy == currentUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

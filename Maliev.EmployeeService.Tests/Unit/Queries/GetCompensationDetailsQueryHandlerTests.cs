using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Queries;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Queries;

/// <summary>
/// Unit tests for GetCompensationDetailsQueryHandler
/// Tests authorization checks for sensitive compensation data access
/// </summary>
public class GetCompensationDetailsQueryHandlerTests
{
    private readonly Mock<ICompensationRepository> _mockCompensationRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetCompensationDetailsQueryHandler _handler;

    public GetCompensationDetailsQueryHandlerTests()
    {
        _mockCompensationRepository = new Mock<ICompensationRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _handler = new GetCompensationDetailsQueryHandler(
            _mockCompensationRepository.Object,
            _mockEmployeeRepository.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_WithHRSpecialist_ShouldAllowAccess()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var compensationRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            SalaryAmount = "85000.00", // Decrypted by interceptor in real scenario
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Annual review",
            CreatedDate = DateTime.UtcNow
        };

        _mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compensationRecord);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employeeId, result!.EmployeeId);
        Assert.Equal(85000.00m, result.SalaryAmount);
        Assert.Equal("THB", result.Currency);
    }

    [Fact]
    public async Task HandleAsync_WithHRGeneralist_ShouldAllowAccess()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var compensationRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            SalaryAmount = "95000.00",
            Currency = "THB",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Promotion",
            CreatedDate = DateTime.UtcNow
        };

        _mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsInRole("HRGeneralist")).Returns(true);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compensationRecord);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(95000.00m, result!.SalaryAmount);
    }

    [Fact]
    public async Task HandleAsync_WithSystemAdministrator_ShouldAllowAccess()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var compensationRecord = new CompensationRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            SalaryAmount = "120000.00",
            Currency = "USD",
            EffectiveDate = DateTime.UtcNow,
            ChangeReason = "Executive compensation",
            CreatedDate = DateTime.UtcNow
        };

        _mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsInRole("HRGeneralist")).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsInRole("SystemAdministrator")).Returns(true);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compensationRecord);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(120000.00m, result!.SalaryAmount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task HandleAsync_WithRegularEmployee_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        _mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsInRole("HRGeneralist")).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsInRole("SystemAdministrator")).Returns(false);

        // Act
        var act = async () => await _handler.HandleAsync(query);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);

        // Verify repositories were never called
        _mockEmployeeRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockCompensationRepository.Verify(
            x => x.GetCurrentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithManager_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        _mockCurrentUserService.Setup(x => x.IsInRole("Manager")).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsInRole("HRGeneralist")).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsInRole("SystemAdministrator")).Returns(false);

        // Act
        var act = async () => await _handler.HandleAsync(query);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);

        _mockEmployeeRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentEmployee_ShouldReturnNull()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        _mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Null(result);

        // Verify compensation repository was not called
        _mockCompensationRepository.Verify(
            x => x.GetCurrentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNoCompensationRecord_ShouldReturnNull()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var query = new GetCompensationDetailsQuery(employeeId);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP004",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        _mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompensationRecord?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAllCompensationDetails()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var recordId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var effectiveDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var createdDate = new DateTime(2023, 12, 15, 0, 0, 0, DateTimeKind.Utc);

        var query = new GetCompensationDetailsQuery(employeeId);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP005",
            EmploymentStatus = EmploymentStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        var compensationRecord = new CompensationRecord
        {
            Id = recordId,
            EmployeeId = employeeId,
            SalaryAmount = "150000.00",
            Currency = "USD",
            EffectiveDate = effectiveDate,
            ChangeReason = "Annual performance review and promotion",
            BonusStructure = "15% annual bonus based on company performance",
            CommissionStructure = "3% on all sales, 5% on sales exceeding quota",
            CreatedDate = createdDate,
            CreatedBy = creatorId
        };

        _mockCurrentUserService.Setup(x => x.IsInRole("HRSpecialist")).Returns(true);
        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _mockCompensationRepository.Setup(x => x.GetCurrentAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compensationRecord);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recordId, result!.Id);
        Assert.Equal(employeeId, result.EmployeeId);
        Assert.Equal(150000.00m, result.SalaryAmount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(effectiveDate, result.EffectiveDate);
        Assert.Equal("Annual performance review and promotion", result.ChangeReason);
        Assert.Equal("15% annual bonus based on company performance", result.BonusStructure);
        Assert.Equal("3% on all sales, 5% on sales exceeding quota", result.CommissionStructure);
        Assert.Equal(createdDate, result.CreatedDate);
        Assert.Equal(creatorId, result.CreatedBy);
    }
}

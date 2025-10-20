using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

public class TransferDepartmentCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IDepartmentRepository> _mockDepartmentRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly TransferDepartmentCommandHandler _handler;

    public TransferDepartmentCommandHandlerTests()
    {
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockDepartmentRepository = new Mock<IDepartmentRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockEventPublisher = new Mock<IEventPublisher>();

        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        _handler = new TransferDepartmentCommandHandler(
            _mockEmployeeRepository.Object,
            _mockDepartmentRepository.Object,
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockEventPublisher.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidTransfer_ShouldSucceed()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var oldDepartmentId = Guid.NewGuid();
        var newDepartmentId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName("John", "Doe"),
            DepartmentId = oldDepartmentId,
            EmploymentStatus = EmploymentStatus.Active
        };

        var newDepartment = new Department
        {
            Id = newDepartmentId,
            Name = "New Department",
            HeadcountLimit = 50,
            IsActive = true,
            Employees = new List<Employee>() // Empty, under capacity
        };

        var command = new TransferDepartmentCommand(
            employeeId,
            newDepartmentId,
            "Promotion",
            DateTime.UtcNow);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(newDepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newDepartment);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        employee.DepartmentId.Should().Be(newDepartmentId);
        employee.ModifiedBy.Should().NotBeNull();
        employee.ModifiedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockEmployeeRepository.Verify(x => x.Update(employee), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentEmployee_ShouldReturnError()
    {
        // Arrange
        var command = new TransferDepartmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Promotion",
            DateTime.UtcNow);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Employee not found");

        _mockEmployeeRepository.Verify(x => x.Update(It.IsAny<Employee>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveEmployee_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Terminated
        };

        var command = new TransferDepartmentCommand(
            employeeId,
            Guid.NewGuid(),
            "Transfer",
            DateTime.UtcNow);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive employee");
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentDepartment_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active
        };

        var command = new TransferDepartmentCommand(
            employeeId,
            Guid.NewGuid(),
            "Transfer",
            DateTime.UtcNow);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Target department not found");
    }

    [Fact]
    public async Task HandleAsync_WithInactiveDepartment_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Active
        };

        var department = new Department
        {
            Id = departmentId,
            Name = "Inactive Department",
            IsActive = false
        };

        var command = new TransferDepartmentCommand(
            employeeId,
            departmentId,
            "Transfer",
            DateTime.UtcNow);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive department");
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyInDepartment_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            DepartmentId = departmentId,
            EmploymentStatus = EmploymentStatus.Active
        };

        var department = new Department
        {
            Id = departmentId,
            Name = "Same Department",
            IsActive = true
        };

        var command = new TransferDepartmentCommand(
            employeeId,
            departmentId,
            "Transfer",
            DateTime.UtcNow);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already in this department");
    }

    [Fact]
    public async Task HandleAsync_WhenDepartmentAtCapacity_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            DepartmentId = Guid.NewGuid(),
            EmploymentStatus = EmploymentStatus.Active
        };

        // Department at capacity (50/50)
        var department = new Department
        {
            Id = departmentId,
            Name = "Full Department",
            HeadcountLimit = 50,
            IsActive = true,
            Employees = Enumerable.Range(1, 50).Select(i => new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = $"EMP{i:000}",
                EmploymentStatus = EmploymentStatus.Active
            }).ToList()
        };

        var command = new TransferDepartmentCommand(
            employeeId,
            departmentId,
            "Transfer",
            DateTime.UtcNow);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("headcount limit");
        result.ErrorMessage.Should().Contain("50");
    }

    [Fact]
    public async Task HandleAsync_WithPastEffectiveDate_ShouldReturnError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            DepartmentId = Guid.NewGuid(), // Different department
            EmploymentStatus = EmploymentStatus.Active
        };

        var department = new Department
        {
            Id = departmentId,
            Name = "Target Department",
            IsActive = true,
            Employees = new List<Employee>()
        };

        var command = new TransferDepartmentCommand(
            employeeId,
            departmentId,
            "Transfer",
            DateTime.UtcNow.AddDays(-1)); // Past date

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("past");
    }
}

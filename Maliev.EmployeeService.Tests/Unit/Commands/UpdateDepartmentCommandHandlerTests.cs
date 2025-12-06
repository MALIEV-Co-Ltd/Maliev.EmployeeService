using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for UpdateDepartmentCommandHandler (User Story 5)
/// </summary>
public class UpdateDepartmentCommandHandlerTests
{
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateDepartmentCommandHandler _handler;

    public UpdateDepartmentCommandHandlerTests()
    {
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateDepartmentCommandHandler(
            _departmentRepositoryMock.Object,
            _employeeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_DepartmentNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateDepartmentCommand
        {
            DepartmentId = Guid.NewGuid(),
            Name = "Updated Department",
            IsActive = true
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(command.DepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Department not found", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_DeactivateWithActiveEmployees_ShouldReturnFailure()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = departmentId,
            Name = "Engineering",
            IsActive = false // Trying to deactivate
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.GetCountByDepartmentAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5); // 5 active employees

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot deactivate department with 5 active employee(s)", result.ErrorMessage);
        Assert.Contains("reassign employees first", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_DeactivateWithNoEmployees_ShouldSucceed()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = departmentId,
            Name = "Engineering",
            IsActive = false
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.GetCountByDepartmentAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.False(department.IsActive);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_HeadcountLimitBelowCurrentHeadcount_ShouldReturnFailure()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = departmentId,
            Name = "Engineering",
            HeadcountLimit = 5, // Trying to set limit to 5
            IsActive = true
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.GetCountByDepartmentAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10); // Current headcount is 10

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot set headcount limit (5) below current headcount (10)", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_HeadcountLimitAt80Percent_ShouldReturnWarning()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = departmentId,
            Name = "Engineering",
            HeadcountLimit = 10, // Setting limit to 10
            IsActive = true
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.GetCountByDepartmentAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8); // 8/10 = 80% capacity

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Warnings);
        Assert.Contains("80.0% capacity", result.Warnings[0]);
        Assert.Contains("8/10", result.Warnings[0]);
    }

    [Fact]
    public async Task HandleAsync_ParentDepartmentNotFound_ShouldReturnFailure()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var parentDepartmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = departmentId,
            Name = "Engineering",
            ParentDepartmentId = parentDepartmentId,
            IsActive = true
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(parentDepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        _employeeRepositoryMock
            .Setup(x => x.GetCountByDepartmentAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Parent department not found", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_CircularReference_ShouldReturnFailure()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true
        };

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = departmentId,
            Name = "Engineering",
            ParentDepartmentId = departmentId, // Circular reference
            IsActive = true
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.GetCountByDepartmentAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Department cannot be its own parent", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ValidUpdate_ShouldSucceed()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var command = new UpdateDepartmentCommand
        {
            DepartmentId = departmentId,
            Name = "Updated Engineering",
            Description = "New description",
            HeadcountLimit = 50,
            IsActive = true
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.GetCountByDepartmentAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Updated Engineering", department.Name);
        Assert.Equal("New description", department.Description);
        Assert.Equal(50, department.HeadcountLimit);
        Assert.True(department.IsActive);
        Assert.NotNull(department.ModifiedDate);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

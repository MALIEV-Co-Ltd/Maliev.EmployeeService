using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.DTOs;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

public class CreateDepartmentCommandHandlerTests
{
    private readonly Mock<IDepartmentRepository> _mockDepartmentRepository;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly CreateDepartmentCommandHandler _handler;

    public CreateDepartmentCommandHandlerTests()
    {
        _mockDepartmentRepository = new Mock<IDepartmentRepository>();
        _mockEmployeeRepository = new Mock<IEmployeeRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockCurrentUserService.Setup(x => x.EmployeeId).Returns(Guid.NewGuid());

        _handler = new CreateDepartmentCommandHandler(
            _mockDepartmentRepository.Object,
            _mockEmployeeRepository.Object,
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldCreateDepartment()
    {
        // Arrange
        var dto = new CreateDepartmentDto
        {
            Name = "Engineering",
            Description = "Engineering department",
            CostCenter = "ENG001",
            HeadcountLimit = 50
        };

        var command = new CreateDepartmentCommand(dto);

        _mockDepartmentRepository.Setup(x => x.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.DepartmentId.Should().NotBeEmpty();
        result.ErrorMessage.Should().BeNull();

        _mockDepartmentRepository.Verify(x => x.AddAsync(
            It.Is<Department>(d =>
                d.Name == "Engineering" &&
                d.Description == "Engineering department" &&
                d.CostCenter == "ENG001" &&
                d.HeadcountLimit == 50 &&
                d.IsActive == true),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidParentDepartment_ShouldReturnError()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var dto = new CreateDepartmentDto
        {
            Name = "Sub-Engineering",
            ParentDepartmentId = parentId
        };

        var command = new CreateDepartmentCommand(dto);

        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.DepartmentId.Should().BeNull();
        result.ErrorMessage.Should().Contain("Parent department");
        result.ErrorMessage.Should().Contain("not found");

        _mockDepartmentRepository.Verify(x => x.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInactiveParentDepartment_ShouldReturnError()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentDepartment = new Department
        {
            Id = parentId,
            Name = "Parent",
            IsActive = false
        };

        var dto = new CreateDepartmentDto
        {
            Name = "Sub-Engineering",
            ParentDepartmentId = parentId
        };

        var command = new CreateDepartmentCommand(dto);

        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentDepartment);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive parent department");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidDepartmentHead_ShouldReturnError()
    {
        // Arrange
        var departmentHeadId = Guid.NewGuid();
        var dto = new CreateDepartmentDto
        {
            Name = "Engineering",
            DepartmentHeadId = departmentHeadId
        };

        var command = new CreateDepartmentCommand(dto);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(departmentHeadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Department head employee");
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task HandleAsync_WithInactiveDepartmentHead_ShouldReturnError()
    {
        // Arrange
        var departmentHeadId = Guid.NewGuid();
        var departmentHead = new Employee
        {
            Id = departmentHeadId,
            EmployeeNumber = "EMP001",
            EmploymentStatus = EmploymentStatus.Terminated
        };

        var dto = new CreateDepartmentDto
        {
            Name = "Engineering",
            DepartmentHeadId = departmentHeadId
        };

        var command = new CreateDepartmentCommand(dto);

        _mockEmployeeRepository.Setup(x => x.GetByIdAsync(departmentHeadId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(departmentHead);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive employee");
    }

    [Fact]
    public async Task HandleAsync_WithCircularHierarchy_ShouldReturnError()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var grandParentId = Guid.NewGuid();

        var grandParent = new Department
        {
            Id = grandParentId,
            Name = "GrandParent",
            ParentDepartmentId = parentId, // This creates a circular reference
            IsActive = true
        };

        var parent = new Department
        {
            Id = parentId,
            Name = "Parent",
            ParentDepartmentId = grandParentId,
            IsActive = true
        };

        var dto = new CreateDepartmentDto
        {
            Name = "Child",
            ParentDepartmentId = parentId
        };

        var command = new CreateDepartmentCommand(dto);

        // Setup the mock to return departments for circular detection
        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);

        _mockDepartmentRepository.Setup(x => x.GetByIdAsync(grandParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grandParent);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("circular hierarchy");

        _mockDepartmentRepository.Verify(x => x.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

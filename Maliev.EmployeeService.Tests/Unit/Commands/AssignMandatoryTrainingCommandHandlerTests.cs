using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.Enums;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for AssignMandatoryTrainingCommandHandler (T285)
/// Tests employment type-based mandatory training assignment logic
/// </summary>
public class AssignMandatoryTrainingCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<IMandatoryTrainingRepository> _mandatoryTrainingRepositoryMock;
    private readonly Mock<ILogger<AssignMandatoryTrainingCommandHandler>> _loggerMock;
    private readonly AssignMandatoryTrainingCommandHandler _handler;

    public AssignMandatoryTrainingCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _mandatoryTrainingRepositoryMock = new Mock<IMandatoryTrainingRepository>();
        _loggerMock = new Mock<ILogger<AssignMandatoryTrainingCommandHandler>>();

        _handler = new AssignMandatoryTrainingCommandHandler(
            _employeeRepositoryMock.Object,
            _mandatoryTrainingRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_FullTimeEmployee_AssignsCorrectMandatoryTraining()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName("John", "Doe", null),
            EmploymentType = EmploymentType.FullTime,
            JobTitle = "Software Engineer",
            StartDate = DateTime.UtcNow.AddDays(-1)
        };

        var fullTimeRequirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.FullTime,
            JobRole = null,
            RequiredCourses = "Safety Training,Code of Conduct,Data Privacy",
            DeadlineDaysFromStart = 30,
            IsActive = true,
            Priority = 1
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mandatoryTrainingRepositoryMock
            .Setup(x => x.GetActiveRequirementsForEmployeeAsync(
                EmploymentType.FullTime,
                "Software Engineer",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { fullTimeRequirement });

        var command = new AssignMandatoryTrainingCommand { EmployeeId = employeeId };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Contains("Safety Training", result);
        Assert.Contains("Code of Conduct", result);
        Assert.Contains("Data Privacy", result);
    }

    [Fact]
    public async Task HandleAsync_ContractorEmployee_AssignsDifferentTraining()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "CON001",
            LegalName = new LegalName("Jane", "Smith", null),
            EmploymentType = EmploymentType.Contractor,
            JobTitle = "Consultant",
            StartDate = DateTime.UtcNow.AddDays(-1)
        };

        var contractorRequirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.Contractor,
            JobRole = null,
            RequiredCourses = "Contractor Safety,NDA Training",
            DeadlineDaysFromStart = 7,
            IsActive = true,
            Priority = 1
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mandatoryTrainingRepositoryMock
            .Setup(x => x.GetActiveRequirementsForEmployeeAsync(
                EmploymentType.Contractor,
                "Consultant",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { contractorRequirement });

        var command = new AssignMandatoryTrainingCommand { EmployeeId = employeeId };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains("Contractor Safety", result);
        Assert.Contains("NDA Training", result);
    }

    [Fact]
    public async Task HandleAsync_RoleSpecificTraining_AssignsBasedOnJobTitle()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            LegalName = new LegalName("Bob", "Manager", null),
            EmploymentType = EmploymentType.FullTime,
            JobTitle = "Manager",
            StartDate = DateTime.UtcNow.AddDays(-1)
        };

        var generalRequirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.FullTime,
            JobRole = null,
            RequiredCourses = "Safety Training,Code of Conduct",
            DeadlineDaysFromStart = 30,
            IsActive = true,
            Priority = 1
        };

        var managerRequirement = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = null,
            JobRole = "Manager",
            RequiredCourses = "Leadership Training,Performance Management",
            DeadlineDaysFromStart = 60,
            IsActive = true,
            Priority = 2
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mandatoryTrainingRepositoryMock
            .Setup(x => x.GetActiveRequirementsForEmployeeAsync(
                EmploymentType.FullTime,
                "Manager",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { generalRequirement, managerRequirement });

        var command = new AssignMandatoryTrainingCommand { EmployeeId = employeeId };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(4, result.Count());
        Assert.Contains("Safety Training", result);
        Assert.Contains("Code of Conduct", result);
        Assert.Contains("Leadership Training", result);
        Assert.Contains("Performance Management", result);
    }

    [Fact]
    public async Task HandleAsync_NoRequirements_ReturnsEmptyList()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            LegalName = new LegalName("Alice", "Worker", null),
            EmploymentType = EmploymentType.PartTime,
            JobTitle = "Intern",
            StartDate = DateTime.UtcNow.AddDays(-1)
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mandatoryTrainingRepositoryMock
            .Setup(x => x.GetActiveRequirementsForEmployeeAsync(
                EmploymentType.PartTime,
                "Intern",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<MandatoryTrainingRequirement>());

        var command = new AssignMandatoryTrainingCommand { EmployeeId = employeeId };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_EmployeeNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new AssignMandatoryTrainingCommand { EmployeeId = employeeId };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_MultipleRequirementsWithPriority_AssignsInPriorityOrder()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP004",
            LegalName = new LegalName("Charlie", "Developer", null),
            EmploymentType = EmploymentType.FullTime,
            JobTitle = "Senior Developer",
            StartDate = DateTime.UtcNow.AddDays(-1)
        };

        var requirement1 = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = EmploymentType.FullTime,
            JobRole = null,
            RequiredCourses = "Basic Training",
            DeadlineDaysFromStart = 7,
            IsActive = true,
            Priority = 1
        };

        var requirement2 = new MandatoryTrainingRequirement
        {
            Id = Guid.NewGuid(),
            EmploymentType = null,
            JobRole = "Senior Developer",
            RequiredCourses = "Advanced Training",
            DeadlineDaysFromStart = 30,
            IsActive = true,
            Priority = 2
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _mandatoryTrainingRepositoryMock
            .Setup(x => x.GetActiveRequirementsForEmployeeAsync(
                EmploymentType.FullTime,
                "Senior Developer",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { requirement1, requirement2 });

        var command = new AssignMandatoryTrainingCommand { EmployeeId = employeeId };

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains("Basic Training", result);
        Assert.Contains("Advanced Training", result);
    }
}

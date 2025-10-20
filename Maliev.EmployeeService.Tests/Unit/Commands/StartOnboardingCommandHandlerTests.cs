using FluentAssertions;
using Maliev.EmployeeService.Application.Commands;
using Maliev.EmployeeService.Application.IntegrationEvents;
using Maliev.EmployeeService.Application.Interfaces;
using Maliev.EmployeeService.Application.Services;
using Maliev.EmployeeService.Domain.Entities;
using Maliev.EmployeeService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.EmployeeService.Tests.Unit.Commands;

/// <summary>
/// Unit tests for StartOnboardingCommandHandler
/// Verifies onboarding workflow initialization and integration event publishing
/// </summary>
public class StartOnboardingCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<IOnboardingRepository> _onboardingRepositoryMock;
    private readonly OnboardingTemplateService _templateService;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<StartOnboardingCommandHandler>> _loggerMock;
    private readonly StartOnboardingCommandHandler _handler;

    public StartOnboardingCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _onboardingRepositoryMock = new Mock<IOnboardingRepository>();
        _templateService = new OnboardingTemplateService();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<StartOnboardingCommandHandler>>();

        _handler = new StartOnboardingCommandHandler(
            _employeeRepositoryMock.Object,
            _onboardingRepositoryMock.Object,
            _templateService,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidEmployee_CreatesChecklistAndPublishesEvent()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP001",
            LegalName = new LegalName { FirstName = "John", LastName = "Doe" },
            ContactInformation = new ContactInformation { WorkEmail = "john.doe@company.com" },
            JobTitle = "Software Engineer",
            StartDate = startDate,
            ManagerId = managerId,
            DepartmentId = departmentId,
            Department = new Department
            {
                Id = departmentId,
                Name = "Engineering"
            }
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _onboardingRepositoryMock
            .Setup(x => x.CreateChecklistAsync(It.IsAny<IEnumerable<OnboardingChecklist>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOnboardingCommand { EmployeeId = employeeId };

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().Be(employeeId);

        // Verify checklist creation
        _onboardingRepositoryMock.Verify(
            x => x.CreateChecklistAsync(
                It.Is<IEnumerable<OnboardingChecklist>>(checklist => checklist.Any()),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify integration event published
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmployeeOnboardingStartedIntegrationEvent>(evt =>
                    evt.EmployeeId == employeeId &&
                    evt.EmployeeNumber == "EMP001" &&
                    evt.FullName == "John Doe" &&
                    evt.Email == "john.doe@company.com" &&
                    evt.StartDate == startDate &&
                    evt.Department == "Engineering" &&
                    evt.JobTitle == "Software Engineer" &&
                    evt.ManagerId == managerId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmployeeNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new StartOnboardingCommand { EmployeeId = employeeId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        exception.Message.Should().Contain(employeeId.ToString());

        // Verify no checklist or event created
        _onboardingRepositoryMock.Verify(
            x => x.CreateChecklistAsync(It.IsAny<IEnumerable<OnboardingChecklist>>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_EmployeeWithNoDepartment_UsesUnknownDepartment()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP002",
            LegalName = new LegalName { FirstName = "Jane", LastName = "Smith" },
            ContactInformation = new ContactInformation { WorkEmail = "jane.smith@company.com" },
            JobTitle = "Consultant",
            StartDate = startDate,
            Department = null // No department assigned
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _onboardingRepositoryMock
            .Setup(x => x.CreateChecklistAsync(It.IsAny<IEnumerable<OnboardingChecklist>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOnboardingCommand { EmployeeId = employeeId };

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmployeeOnboardingStartedIntegrationEvent>(evt =>
                    evt.Department == "Unknown"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmployeeWithNoJobTitle_UsesDefaultEmployee()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP003",
            LegalName = new LegalName { FirstName = "Bob", LastName = "Johnson" },
            ContactInformation = new ContactInformation { WorkEmail = "bob.johnson@company.com" },
            JobTitle = null, // No job title
            StartDate = startDate
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _onboardingRepositoryMock
            .Setup(x => x.CreateChecklistAsync(It.IsAny<IEnumerable<OnboardingChecklist>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOnboardingCommand { EmployeeId = employeeId };

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<EmployeeOnboardingStartedIntegrationEvent>(evt =>
                    evt.JobTitle == "Employee"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ManagerEmployee_CreatesChecklistWithManagerItems()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.Date.AddDays(7);

        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "MGR001",
            LegalName = new LegalName { FirstName = "Alice", LastName = "Manager" },
            ContactInformation = new ContactInformation { WorkEmail = "alice.manager@company.com" },
            JobTitle = "Engineering Manager",
            StartDate = startDate
        };

        _employeeRepositoryMock
            .Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        OnboardingChecklist[]? capturedChecklist = null;
        _onboardingRepositoryMock
            .Setup(x => x.CreateChecklistAsync(It.IsAny<IEnumerable<OnboardingChecklist>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<OnboardingChecklist>, CancellationToken>((checklist, ct) =>
            {
                capturedChecklist = checklist.ToArray();
            })
            .Returns(Task.CompletedTask);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EmployeeOnboardingStartedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new StartOnboardingCommand { EmployeeId = employeeId };

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        capturedChecklist.Should().NotBeNull();
        capturedChecklist.Should().Contain(item => item.ItemDescription.Contains("leadership"));
    }
}
